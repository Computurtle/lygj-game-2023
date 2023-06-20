using System.Collections.Generic;
using System.Linq;
using LYGJ.Common;
using LYGJ.InventoryManagement;
using UnityEditor;
using UnityEngine;

namespace LYGJ {
    public static class PrototypeItemGenerator {

        [MenuItem("Tools/Prototype/Generate Item(s)", false)]
        public static void GenerateItems() {
            Texture2D[]           Textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            IReadOnlyList<Sprite> Sprites  = GetSprites(Textures);
            if (Sprites.Count == 0) {
                Debug.LogWarning("No sprites found in selection.");
                return;
            }

            List<Object> Generated = new(Sprites.Count);
            string       Folder    = AssetDatabase.GetAssetPath(Selection.activeObject);
            Folder = Folder[..Folder.LastIndexOf('/')];
            foreach (Sprite Sprite in Sprites) {
                Item Item = ScriptableObject.CreateInstance<Item>();
                Item.Icon = Sprite;
                string Name = Sprite.name.ConvertNamingConvention(NamingConvention.TitleCase);
                if (char.IsDigit(Name[0])) {
                    int NumChars = 0;
                    foreach (char C in Name) {
                        if (char.IsDigit(C)) {
                            NumChars++;
                        } else {
                            break;
                        }
                    }

                    Name = Name[NumChars..].Trim();
                }

                Item.Name = Name;
                string ID = Name.ConvertNamingConvention(NamingConvention.KebabCase);
                Item.ID = ID;
                string Out = $"{Folder}/{ID}.asset";
                Out = AssetDatabase.GenerateUniqueAssetPath(Out);
                AssetDatabase.CreateAsset(Item, Out);
                Generated.Add(Item);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.objects = Generated.ToArray();
        }

        [MenuItem("Tools/Prototype/Generate Item(s)", true)]
        public static bool ValidateGenerateItems() => Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;

        public static IReadOnlyList<Sprite> GetSprites( IEnumerable<Texture2D> Textures ) {
            List<Sprite> Sprites = new();

            foreach (Texture2D Texture in Textures) {
                string   PATH   = AssetDatabase.GetAssetPath(Texture);
                Object[] Assets = AssetDatabase.LoadAllAssetsAtPath(PATH);

                foreach (Object Asset in Assets) {
                    if (Asset is Sprite Sprite && IsSpriteMarked(Sprite)) {
                        Sprites.Add(Sprite);
                    }
                }
            }

            return Sprites;
        }

        static bool IsSpriteMarked( Sprite Sprite ) {
            TextureImporter? Importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Sprite)) as TextureImporter;
            if (Importer != null) {
                TextureImporterSettings Settings = new();
                Importer.ReadTextureSettings(Settings);
                return Settings.spriteMode != (int)SpriteImportMode.None;
            }

            return false;
        }

        [MenuItem("Tools/Prototype/Add all Items to Inventory", false)]
        public static void AddAllItemsToInventory() {
            Item[] Items = AssetDatabase.FindAssets("t:Item")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Item>)
                .ToArray();
            const int Min = 1, Max = 99;
            foreach (Item Item in Items) {
                Inventory.Add(Item, (uint)Random.Range(Min, Max));
            }
        }

    }
}
