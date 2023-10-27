using ClickableTransparentOverlay;
using CS2_External;
using ImGuiNET;
using SixLabors.ImageSharp.Metadata;
using Swed64;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.MetalBindings;
using Vortice.Direct3D11;

namespace CS2External
{
    class Program : Overlay
    {

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]

        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]

        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public RECT GetWindowRect(IntPtr hWnd)
        {
            RECT rect = new RECT();
            GetWindowRect(hWnd, out rect);
            return rect;
        }

        Swed swed = new Swed("cs2");
        Offsets offsets = new Offsets();
        ImDrawListPtr drawList;

        Entity localPlayer = new Entity();
        List<Entity> entities = new List<Entity>();
        List<Entity> enemyTeam = new List<Entity>();
        List<Entity> playerTeam = new List<Entity>();

        IntPtr client;

        const int AIMBOT_HOTKEY = 0x06;

        Vector3 offsetVector = new Vector3(0,0,10);

        Vector4 teamColor = new Vector4(0, 0, 1, 1);
        Vector4 enemyColor = new Vector4(0, 0, 0, 1);
        Vector4 healthBarColor = new Vector4(0, 1, 0, 1);
        Vector4 healthTextColor = new Vector4(0, 0, 0, 1);

        Vector2 windowLocation = new Vector2(0, 0);
        Vector2 windowSize = new Vector2(1920, 1080);
        Vector2 lineOrigin = new Vector2(1920 / 2, 1080);
        Vector2 windowCenter = new Vector2(1920 / 2, 1080 / 2);

        bool enableESP = true;
        bool enableAimbot = true;
        bool enableAimbotCrosshair = false;

        bool enableTeamChek = false;
        bool enableTeamLine = true;
        bool enableTeamBox = true;
        bool enableTeamDot = false;
        bool enableTeamHealthBar = true;
        bool enableTeamDistance = true;

        bool enableEnemyLine = true;
        bool enableEnemyBox = true;
        bool enableEnemyDot = false;
        bool enableEnemyHealthBar = true;
        bool enableEnemyDistance = true;

        protected override void Render()
        {
            DrawMenu();
            DrawOverlay();
            Esp();
            ImGui.End();
        }

        void Aimbot()
        {
            if (GetAsyncKeyState(AIMBOT_HOTKEY) < 0 && enableAimbot)
            {
                if (enemyTeam.Count > 0)
                {
                    var angles = CalculateAngles(localPlayer.origin, Vector3.Subtract(enemyTeam[0].origin, offsetVector));
                    AimAt(angles);
                }
            }
        }

        void AimAt(Vector3 angles)
        {
            swed.WriteFloat(client, offsets.viewAngle, angles.Y);
            swed.WriteFloat(client, offsets.viewAngle + 0x4, angles.X);
        }

        Vector3 CalculateAngles(Vector3 from, Vector3 destintion)
        {
            float yaw;
            float pitch;

            float deltaX = destintion.X - from.X;
            float deltaY = destintion.Y - from.Y;
            yaw = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

            float deltaZ = destintion.Z - from.Z;
            double distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            pitch = -(float)(Math.Atan2(deltaZ, distance) * 180 / Math.PI);

            return new Vector3(yaw, pitch, 0);
        }

        float CalculatePixelDistance(Vector2 v1, Vector2 v2)
        {
            return (float)Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2));
        }

        float CalculateMagnitude(Vector3 v1, Vector3 v2)
        {
            return (float)Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2) + Math.Pow(v2.Z - v1.Z, 2));
        }

        void Esp()
        {
            drawList = ImGui.GetWindowDrawList();

            if (enableESP)
            {
                try
                {
                    foreach (var entity in entities)
                    {
                        if (entity.teamNum == localPlayer.teamNum)
                        {
                            DrawVisual(entity, teamColor, enableTeamLine, enableTeamBox, enableTeamDot, enableTeamHealthBar, enableTeamDistance);
                        }
                        else
                        {
                            DrawVisual(entity, enemyColor, enableEnemyLine, enableEnemyBox, enableEnemyDot, enableEnemyHealthBar,enableEnemyDistance);
                        }
                    }
                }catch { }
            }
        }

        void DrawVisual(Entity entity, Vector4 color, bool line, bool box, bool dot, bool healthBar, bool distance)
        {

            if (IsPixelInsideScreen(entity.originScreeenPosition))
            {

                uint uintColor = ImGui.ColorConvertFloat4ToU32(color);
                uint uintHealthTextColor = ImGui.ColorConvertFloat4ToU32(healthTextColor);
                uint uintHealthBarColor = ImGui.ColorConvertFloat4ToU32(healthBarColor);


                Vector2 boxWidth = new Vector2((entity.originScreeenPosition.Y - entity.absScreenPosition.Y) / 2, 0f);
                Vector2 boxStart = Vector2.Subtract(entity.absScreenPosition, boxWidth);
                Vector2 boxEnd = Vector2.Add(entity.originScreeenPosition, boxWidth);
                 
                float barPercent = entity.health / 100f;
                Vector2 barHeight = new Vector2(0, barPercent * (entity.originScreeenPosition.Y - entity.absScreenPosition.Y));
                Vector2 barStart = Vector2.Subtract(Vector2.Subtract(entity.originScreeenPosition, boxWidth), barHeight);
                Vector2 barEnd = Vector2.Subtract(entity.originScreeenPosition, Vector2.Add(boxWidth, new Vector2(-4, 0)));

                if (line)
                {
                    drawList.AddLine(lineOrigin, entity.originScreeenPosition, uintColor, 3);
                }
                if (box)
                {
                    drawList.AddRect(boxStart, boxEnd, uintColor, 3);
                }
                if (dot)
                {
                    drawList.AddCircleFilled(entity.originScreeenPosition, 5, uintColor);
                }
                if (healthBar)
                {
                    drawList.AddText(entity.originScreeenPosition, uintHealthTextColor, $"hp: {entity.health}");
                    drawList.AddRectFilled(barStart, barEnd, uintHealthBarColor);
                }
            }
        }

        bool IsPixelInsideScreen(Vector2 pixel)
        {
            return pixel.X > windowLocation.X && pixel.X < windowLocation.X + windowSize.X && pixel.Y > windowLocation.Y && pixel.Y < windowSize.Y + windowLocation.Y;
        }

        ViewMatrix ReadMatrix(IntPtr matrixAddress)
        {
            var viewMatrix = new ViewMatrix();
            var floatMatrix = swed.ReadMatrix(matrixAddress);

            viewMatrix.m11 = floatMatrix[0];
            viewMatrix.m12 = floatMatrix[1];
            viewMatrix.m13 = floatMatrix[2];
            viewMatrix.m14 = floatMatrix[3];

            viewMatrix.m21 = floatMatrix[4];
            viewMatrix.m22 = floatMatrix[5];
            viewMatrix.m23 = floatMatrix[6];
            viewMatrix.m24 = floatMatrix[7];

            viewMatrix.m31 = floatMatrix[8];
            viewMatrix.m32 = floatMatrix[9];
            viewMatrix.m33 = floatMatrix[10];
            viewMatrix.m34 = floatMatrix[11];

            viewMatrix.m41 = floatMatrix[12];
            viewMatrix.m42 = floatMatrix[13];
            viewMatrix.m43 = floatMatrix[14];
            viewMatrix.m44 = floatMatrix[15];

            return viewMatrix;
        }

        Vector2 WorldToScreen(ViewMatrix matrix, Vector3 pos, int width, int height)
        {
            Vector2 screenCoordinates = new Vector2();

            float screenW = (matrix.m41 * pos.X) + (matrix.m42 * pos.Y) + (matrix.m43 * pos.Z) + matrix.m44;

            if (screenW > 0.0001f)
            {
                float screenX = (matrix.m11 * pos.X) + (matrix.m12 * pos.Y) + (matrix.m13 * pos.Z) + matrix.m14;

                float screenY = (matrix.m21 * pos.X) + (matrix.m22 * pos.Y) + (matrix.m23 * pos.Z) + matrix.m24;

                float camX = width / 2;
                float camY = height / 2;

                float X = camX + (camX * screenX / screenW);
                float Y = camY - (camY * screenY / screenW);

                screenCoordinates.X = X;
                screenCoordinates.Y = Y;
                return screenCoordinates;
            }
            else
            {
                return new Vector2 (-99, -99);
            }
        }

        void DrawMenu()
        {
            ImGui.Begin("NullReferenceException");

            if (ImGui.BeginTabBar("Tabs"))
            {
                if (ImGui.BeginTabItem("Visual"))
                {
                    ImGui.Checkbox("ESP", ref enableESP);
                    if (enableESP)
                    {
                        ImGui.SameLine();
                        ImGui.Checkbox("Enable team chek", ref enableTeamChek);
                        if (enableTeamChek)
                        {
                            ImGui.SameLine();
                            ImGui.Checkbox("Team line", ref enableTeamLine);
                            ImGui.SameLine();
                            ImGui.Checkbox("Team box", ref enableTeamBox);
                            ImGui.SameLine();
                            ImGui.Checkbox("Team dot", ref enableTeamDot);
                            ImGui.SameLine();
                            ImGui.Checkbox("Team healthBar", ref enableTeamHealthBar);
                        }
                        else
                        {
                            enableTeamChek = false;
                        }

                        ImGui.Checkbox("Enemy line", ref enableEnemyLine);
                        ImGui.Checkbox("Enemy box", ref enableEnemyBox);
                        ImGui.Checkbox("Enemy dot", ref enableEnemyDot);
                        ImGui.Checkbox("Enemy HealthBar", ref enableEnemyHealthBar);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Aimbot"))
                {
                    ImGui.Checkbox("AIM", ref enableAimbot);

                    if (enableAimbot)
                    {
                        ImGui.SameLine();
                        ImGui.Checkbox("Closest to crosshair", ref enableAimbotCrosshair);
                    }
                    else
                    {
                        enableAimbotCrosshair = false;
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Colors"))
                {
                    ImGui.ColorPicker4("Team color", ref teamColor);

                    ImGui.ColorPicker4("Enemy color", ref enemyColor);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        void DrawOverlay()
        {
            ImGui.SetNextWindowSize(windowSize);
            ImGui.SetNextWindowPos(windowLocation);
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                );
        }

        void MainLogic()
        {

            var window = GetWindowRect(swed.GetProcess().MainWindowHandle);
            windowLocation = new Vector2(window.left, window.top);
            windowSize = Vector2.Subtract(new Vector2(window.right, window.bottom), windowLocation);
            lineOrigin = new Vector2(windowLocation.X + windowSize.X / 2, window.bottom);
            windowCenter = new Vector2(lineOrigin.X, window.bottom - windowSize.Y / 2);


            client = swed.GetModuleBase("client.dll");


            while (true)
            {

                ReloadEntities();

                if (enableAimbot)
                {
                    Aimbot();
                }

                Thread.Sleep(3);

            }
        }

        void ReloadEntities()
        {
            entities.Clear();
            playerTeam.Clear();
            enemyTeam.Clear();

            localPlayer.address = swed.ReadPointer(client, offsets.localPlayer);
            UpdateEntity(localPlayer);


            UpdateEntities();

            enemyTeam = enemyTeam.OrderBy(o => o.magnitude).ToList();

            if (enableAimbotCrosshair)
            {
                enemyTeam = enemyTeam.OrderBy(o => o.angleDifference).ToList();
            }
        }

        void UpdateEntities()
        {
            for (int i = 0; i < 64; i++)
            {
                IntPtr tempEntityAddress = swed.ReadPointer(client, offsets.entytiList + i * 0x08);

                if (tempEntityAddress == IntPtr.Zero)
                    continue;
                

                Entity entity = new Entity();
                entity.address = tempEntityAddress;

                UpdateEntity(entity);

                if (entity.health < 1 || entity.health > 100)
                    continue;


                if (!entities.Any(element => element.origin.X == entity.origin.X))
                {
                    entities.Add(entity);

                    if (entity.teamNum == localPlayer.teamNum)
                    {
                        playerTeam.Add(entity);
                    }
                    else
                    {
                        enemyTeam.Add(entity);
                    }
                }
            }
        }

        void UpdateEntity(Entity entity)
        {
            entity.origin = swed.ReadVec(entity.address, offsets.origin);
            entity.viewOffset = new Vector3(0, 0, 65);
            entity.abs = Vector3.Add(entity.origin, entity.viewOffset);

            var currentViewMatrix = ReadMatrix(client + offsets.viewMatrix);
            entity.originScreeenPosition = Vector2.Add(WorldToScreen(currentViewMatrix, entity.origin, (int)windowSize.X, (int)windowSize.Y), windowLocation);
            entity.absScreenPosition = Vector2.Add(WorldToScreen(currentViewMatrix, entity.abs, (int)windowSize.X, (int)windowSize.Y), windowLocation);

            entity.angleDifference = CalculatePixelDistance(windowCenter, entity.absScreenPosition);
            entity.health = swed.ReadInt(entity.address, offsets.health);
            entity.origin = swed.ReadVec(entity.address, offsets.origin);
            entity.teamNum = swed.ReadInt(entity.address, offsets.teamNum);
            entity.magnitude = CalculateMagnitude(localPlayer.origin, entity.origin);
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Start().Wait();

            Thread mainLogicThread = new Thread(program.MainLogic) { IsBackground = true };
            mainLogicThread.Start();
        }
    }
}