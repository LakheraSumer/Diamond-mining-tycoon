path = 'Assets/_Project/Scripts/Runtime/YardArt.cs'
with open(path, 'r', encoding='utf-8') as f:
    code = f.read()

code_lf = code.replace('\r\n', '\n')

# Find the end of Part method and the class closing brace
target = """            renderer.sharedMaterial = material;
            return block.transform;
        }

    }"""

idx = code_lf.find(target)
if idx != -1:
    # Remove everything from target to the end and rebuild properly
    # First find where extra_methods starts
    vignette_idx = code_lf.find('public static void BuildCaveMineVignette')
    if vignette_idx != -1:
        extra_content = code_lf[vignette_idx:].strip()
        # Remove trailing braces from extra_content if any
        while extra_content.endswith('}'):
            extra_content = extra_content[:-1].strip()
        
        base_code = code_lf[:idx + len("""            renderer.sharedMaterial = material;
            return block.transform;
        }""")]
        
        fixed_code = base_code + "\n\n        " + extra_content + "\n    }\n}\n"
        with open(path, 'w', encoding='utf-8', newline='\r\n') as f:
            f.write(fixed_code)
        print('Fixed YardArt.cs braces')
    else:
        print('Could not find BuildCaveMineVignette')
else:
    print('Could not find target')
