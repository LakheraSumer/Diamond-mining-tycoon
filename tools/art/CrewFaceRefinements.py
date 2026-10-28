"""Tavi/Mira face refinement helpers, retaining the MakeHuman CC0 head topology.

The eye surround is part of the actual head mesh. It shares boundary vertices
and smooth normals with the cheeks, rather than laying a separate skin ring on
top. These helpers are a reconstruction, not a measured likeness claim.
"""
import math
import bmesh
from mathutils import Vector


def sculpt_point(x, h, d, mouth_h):
    if d > .065:
        # Lift the actual anatomical lip corners and cheek tissue together.
        # The previous short, puckered mouth came from stretching only its centre.
        u = min(1., abs(x) / .060)
        mouth_weight = math.exp(-((h-mouth_h)/.027)**2-(x/.087)**6)
        h += .0135 * u**1.65 * mouth_weight
        # Slightly flatten the projecting lower lip; leave real lip topology.
        d -= .0038 * math.exp(-(x/.055)**4-((h-(mouth_h-.011))/.012)**2)
        # Soft cheek pads follow the smile into the outer face.
        d += .006 * math.exp(-((abs(x)-.105)/.052)**2-((h-1.606)/.048)**2)
    # Round the lower silhouette across a broad region, without a hard morph band.
    jaw = math.exp(-((h-1.525)/.045)**2)
    if abs(x) > .052:
        x *= 1 - .075 * jaw
    if d > .05:
        d += .004 * math.exp(-(x/.085)**2-((h-1.510)/.028)**2)
    return x, h, d


def attach_eyelids(head_obj, eyes, radius, bulge, height):
    """Bridge subdivided head eye-hole loops to a shaped eye edge in one mesh."""
    bm = bmesh.new()
    bm.from_mesh(head_obj.data)
    uv = bm.loops.layers.uv.active
    contact = bm.loops.layers.color.get('ContactAO')
    if contact is None:
        contact = bm.loops.layers.float_color.get('ContactAO')
    deform = bm.verts.layers.deform.verify()
    head_group = head_obj.vertex_groups['Head'].index
    for eye in eyes:
        # Work in the generator's normalised x/height/depth coordinates.
        def point(vert):
            co = vert.co / height
            return Vector((co.x, co.z, co.y))
        edges = [edge for edge in bm.edges if edge.is_boundary and all(
            abs(point(vert).x-eye.x) < .085 and abs(point(vert).y-eye.y) < .067 and point(vert).z > .06
            for vert in edge.verts)]
        adjacency = {}
        for edge in edges:
            a, b = edge.verts
            adjacency.setdefault(a, []).append(b)
            adjacency.setdefault(b, []).append(a)
        remaining = set(adjacency)
        loops = []
        while remaining:
            start = next(iter(remaining))
            component = {start}
            pending = [start]
            while pending:
                for neighbour in adjacency[pending.pop()]:
                    if neighbour not in component:
                        component.add(neighbour)
                        pending.append(neighbour)
            remaining -= component
            if len(component) < 8 or any(len(adjacency[v]) != 2 for v in component):
                continue
            loop = [start]
            previous = None
            cursor = start
            while True:
                nxt = next(v for v in adjacency[cursor] if v != previous)
                if nxt == start:
                    break
                loop.append(nxt)
                previous, cursor = cursor, nxt
            loops.append(loop)
        if not loops:
            bm.free()
            raise RuntimeError('Could not identify a closed anatomical eye opening; refusing disconnected lid overlay')
        # The removed eye region contains one loop. Choose the largest if an old
        # tiny tear-duct opening survived the topology's original boundary.
        boundary = max(loops, key=len)
        previous = boundary
        for amount in (.30, .65, 1.0):
            ring = []
            for vertex in boundary:
                p = point(vertex)
                angle = math.atan2((p.y-eye.y)/(.58*radius), (p.x-eye.x)/radius)
                c, s = math.cos(angle), math.sin(angle)
                target = Vector((eye.x+radius*c, eye.y+radius*.58*math.copysign(abs(s)**1.30,s), eye.z+bulge-.009))
                q = p.lerp(target, amount)
                # Soft convex upper lid, no outer overlap seam.
                q.z += .0006 * math.sin(amount*math.pi)
                new = bm.verts.new((q.x*height,q.z*height,q.y*height))
                new[deform][head_group] = 1.
                ring.append(new)
            for i in range(len(ring)):
                face = bm.faces.new((previous[i],previous[(i+1)%len(ring)],ring[(i+1)%len(ring)],ring[i]))
                face.smooth = True
                for loop in face.loops:
                    p = point(loop.vert)
                    loop[uv].uv = (.5+math.atan2(p.x,p.z-.015)/math.tau,(p.y-1.37)/.525)
                    if contact is not None:loop[contact] = (1,1,1,1)
            previous = ring
        print('CONTINUOUS-EYE-LID', head_obj.name, len(boundary), flush=True)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(head_obj.data)
    bm.free()
    head_obj.data.update()
