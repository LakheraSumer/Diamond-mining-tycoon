path = 'Assets/_Project/Scripts/Runtime/YardWorld.cs'
with open(path, 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Add screenMode field and SetScreenMode method
old_prop = """        public void FrameTimberArea(bool enabled) { areaFraming = enabled; cameraReady = false; }"""

new_prop = """        public void FrameTimberArea(bool enabled) { areaFraming = enabled; cameraReady = false; }
        string screenMode = "";
        public void SetScreenMode(string nextMode) { screenMode = nextMode ?? ""; cameraReady = false; }"""

code_lf = code.replace('\r\n', '\n')
old_prop_lf = old_prop.replace('\r\n', '\n')
new_prop_lf = new_prop.replace('\r\n', '\n')

assert old_prop_lf in code_lf, "Could not find FrameTimberArea in YardWorld.cs"
code_lf = code_lf.replace(old_prop_lf, new_prop_lf)

# 2. Update FollowCamera
old_follow = """        void FollowCamera(Vector3 target, bool reducedMotion)
        {
            YardPoint entrance = YardLayout.Entrance(0);
            Vector3 focus = new Vector3((float)entrance.X-.15f, 1.1f, (float)entrance.Z-.28f);
            bool nearTimber = areaFraming || Vector2.Distance(new Vector2(target.x,target.z),
                new Vector2((float)entrance.X,(float)entrance.Z)) < 2.8f;
            if (nearTimber)
            {
                view.orthographic = true;
                view.orthographicSize = Mathf.Max(2.65f,2.65f/Mathf.Max(.3f,view.aspect));
                Vector3 position = focus + new Vector3(4.8f,3.7f,-7.2f);
                cameraPosition = !cameraReady || reducedMotion ? position :
                    Vector3.Lerp(cameraPosition,position,1f-Mathf.Exp(-6f*Time.unscaledDeltaTime));
                cameraReady = true;
                view.transform.position = cameraPosition;
                view.transform.LookAt(focus);
                return;
            }
            view.orthographic = false;
            Vector3 desired = target + new Vector3(-6.5f, 9.5f, -7.2f);
            desired.x = Mathf.Clamp(desired.x, -6f, 6f);
            desired.z = Mathf.Clamp(desired.z, -8f, 12f);
            if (!cameraReady || reducedMotion)
            {
                cameraPosition = desired;
                cameraReady = true;
            }
            else
            {
                cameraPosition = Vector3.Lerp(cameraPosition, desired, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            }
            view.transform.position = cameraPosition;
            view.transform.LookAt(target + new Vector3(0f, 0.8f, 0f));
        }"""

new_follow = """        void FollowCamera(Vector3 target, bool reducedMotion)
        {
            if (screenMode == "mine")
            {
                view.orthographic = false;
                view.fieldOfView = 38f;
                Vector3 focus = new Vector3(-8.2f, 1.15f, 6.8f);
                Vector3 desired = focus + new Vector3(3.4f, 2.4f, -4.6f);
                PositionCamera(desired, focus, reducedMotion);
                return;
            }
            if (screenMode == "factory")
            {
                view.orthographic = false;
                view.fieldOfView = 42f;
                Vector3 focus = new Vector3(2.2f, 0.95f, 2.6f);
                Vector3 desired = focus + new Vector3(-3.2f, 3.8f, -4.8f);
                PositionCamera(desired, focus, reducedMotion);
                return;
            }
            if (screenMode == "craft")
            {
                view.orthographic = false;
                view.fieldOfView = 36f;
                Vector3 focus = new Vector3(6.4f, 0.95f, 0.2f);
                Vector3 desired = focus + new Vector3(-1.8f, 2.1f, -3.2f);
                PositionCamera(desired, focus, reducedMotion);
                return;
            }
            if (screenMode == "shop")
            {
                view.orthographic = false;
                view.fieldOfView = 36f;
                Vector3 focus = new Vector3(4.2f, 1.05f, -1.2f);
                Vector3 desired = focus + new Vector3(-1.6f, 2.1f, -3.4f);
                PositionCamera(desired, focus, reducedMotion);
                return;
            }
            if (screenMode == "map")
            {
                view.orthographic = false;
                view.fieldOfView = 44f;
                Vector3 focus = new Vector3(26f, 0.8f, 26f);
                Vector3 desired = focus + new Vector3(-5.5f, 12f, -8.5f);
                PositionCamera(desired, focus, reducedMotion);
                return;
            }

            // ScreenMode == "" or general Explore / Tasks / Rank / Characters
            YardPoint entrance = YardLayout.Entrance(0);
            Vector3 timberFocus = new Vector3((float)entrance.X - 0.15f, 1.1f, (float)entrance.Z - 0.28f);
            bool nearTimber = areaFraming || Vector2.Distance(new Vector2(target.x, target.z),
                new Vector2((float)entrance.X, (float)entrance.Z)) < 2.8f;
            if (nearTimber)
            {
                view.orthographic = true;
                view.orthographicSize = Mathf.Max(2.65f, 2.65f / Mathf.Max(0.3f, view.aspect));
                Vector3 position = timberFocus + new Vector3(4.8f, 3.7f, -7.2f);
                PositionCamera(position, timberFocus, reducedMotion);
                return;
            }
            view.orthographic = false;
            view.fieldOfView = 42f;
            Vector3 desiredYard = target + new Vector3(-6.5f, 9.5f, -7.2f);
            desiredYard.x = Mathf.Clamp(desiredYard.x, -6f, 6f);
            desiredYard.z = Mathf.Clamp(desiredYard.z, -8f, 12f);
            PositionCamera(desiredYard, target + new Vector3(0f, 0.8f, 0f), reducedMotion);
        }

        void PositionCamera(Vector3 desired, Vector3 lookTarget, bool reducedMotion)
        {
            if (!cameraReady || reducedMotion)
            {
                cameraPosition = desired;
                cameraReady = true;
            }
            else
            {
                cameraPosition = Vector3.Lerp(cameraPosition, desired, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            }
            view.transform.position = cameraPosition;
            view.transform.LookAt(lookTarget);
        }"""

code_lf = code_lf.replace(old_follow.replace('\r\n', '\n'), new_follow.replace('\r\n', '\n'))

with open(path, 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(code_lf)
print('Successfully updated YardWorld.cs')
