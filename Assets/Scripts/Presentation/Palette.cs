using UnityEngine;

namespace NexusBreaker
{
    /// <summary>Docs/ASSET_SPEC.md의 32색 팔레트. UI 색도 여기서만 가져온다.</summary>
    public static class Palette
    {
        static Color32 H(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        // 외곽/암부
        public static readonly Color32 Black = H(0x0f0d0c), Outline = H(0x1e1a17), Shadow = H(0x2e2620);
        // 녹·갈색
        public static readonly Color32 Rust0 = H(0x4a3428), Rust1 = H(0x6b4a35), Rust2 = H(0x8c5f3f), Rust3 = H(0xb07a4f), Rust4 = H(0xd29c6a);
        // 잿빛
        public static readonly Color32 Ash0 = H(0x3b3d40), Ash1 = H(0x575a5c), Ash2 = H(0x7b7d7a), Ash3 = H(0xa3a39b), Ash4 = H(0xc9c6b8), Ash5 = H(0xe8e3d3);
        // 먼지·모래
        public static readonly Color32 Dust0 = H(0x7a6a4f), Dust1 = H(0xa08b63), Dust2 = H(0xc4ad7f);
        // 이끼·독성
        public static readonly Color32 Moss0 = H(0x2f3a24), Moss1 = H(0x4c5c32), Moss2 = H(0x73843f);
        // 녹슨 적색
        public static readonly Color32 Red0 = H(0x5c1f1a), Red1 = H(0x8e2f22), Red2 = H(0xc2482c), Red3 = H(0xe07a3c);
        // 불빛
        public static readonly Color32 Lamp0 = H(0xf2b84b), Lamp1 = H(0xffe08a);
        // 넥서스·외계
        public static readonly Color32 Void = H(0x1b0f3a), Violet = H(0x4b1f8c), Purple = H(0x8a3ff2), Cyan = H(0x2bd9c9), CyanLight = H(0x8ff7e8), Magenta = H(0xf25fd0);

        public static Color32 GradeColor(Grade g) => g switch
        {
            Grade.Common => Ash4,
            Grade.Uncommon => Moss2,
            Grade.Rare => Lamp0,
            Grade.Alien => Cyan,
            _ => Magenta,
        };

        public static string Hex(Color32 c) => $"#{c.r:X2}{c.g:X2}{c.b:X2}";
    }
}
