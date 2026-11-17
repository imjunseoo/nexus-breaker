using UnityEngine;

namespace NexusBreaker
{
    /// <summary>메인 띠 화면(384×96px, PPU 32 → 12×3 유닛) 좌표 규약. Docs/ASSET_SPEC.md 1장과 일치.</summary>
    public static class WorldLayout
    {
        public const int StripW = 384;
        public const int StripH = 96;
        public const int PageH = 120;          // 상점/강화 페이지가 열릴 때 위에 추가되는 높이
        public const float PPU = 32f;

        public static float HalfW => StripW / PPU / 2f;   // 6
        public static float HalfH => StripH / PPU / 2f;   // 1.5
        public static float GroundY => -HalfH + 16f / PPU; // -1.0 (바닥선: 아래에서 16px)

        public static Vector3 NexusPos => new Vector3(-3.0f, GroundY, 0f);
        public static Vector3 NexusCenter => new Vector3(-3.0f, GroundY + 1.4f, 0f);
        public static Vector3 RepairmanPos => new Vector3(-1.62f, GroundY, 0f);
        public static Vector3 StallPos => new Vector3(3.2f, GroundY, 0f);
        public static Vector3 BoxCollectPos => new Vector3(2.6f, GroundY + 0.6f, 0f);
        public static float CustomerSpawnX => HalfW + 0.6f;

        static readonly float[] HelperXs = { -4.55f, -1.05f, -5.15f, -0.55f, -5.65f };
        public static Vector3 HelperPos(int i) => new Vector3(HelperXs[Mathf.Clamp(i, 0, HelperXs.Length - 1)], GroundY, 0f);
        public static bool HelperFacesLeft(int i) => HelperXs[Mathf.Clamp(i, 0, HelperXs.Length - 1)] > NexusPos.x;

        /// <summary>손님 대기 위치(0 = 카운터 앞).</summary>
        public static Vector3 QueuePos(int i) => new Vector3(1.55f + 0.62f * i, GroundY, 0f);

        public static class Order
        {
            public const int Sky = -50, Far = -40, Mid = -30, Ground = -20, Props = -15, Stall = -10;
            public const int Nexus = 0, Helper = 5, Customer = 8, Repairman = 10, Box = 12, Fx = 20, Debris = 21;
        }

        /// <summary>월드 좌표를 픽셀 격자에 맞춘다(지글거림 방지).</summary>
        public static Vector3 Snap(Vector3 p) =>
            new Vector3(Mathf.Round(p.x * PPU) / PPU, Mathf.Round(p.y * PPU) / PPU, p.z);
    }
}
