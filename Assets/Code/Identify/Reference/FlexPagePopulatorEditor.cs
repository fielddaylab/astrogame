using UnityEditor;
using UnityEngine;

namespace Astro {
    [CustomEditor(typeof (FlexPagePopulator)), CanEditMultipleObjects]
    public class FlexPagePopulatorEditor : Editor {


        public override void OnInspectorGUI () {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            if (GUILayout.Button("Populate")) {
                FlexPageUtility.PressTestButton((FlexPagePopulator) target);
            }
        }

    }

    public class FlexPageUtility {
        public static void PressTestButton (FlexPagePopulator target) {
            if (target.RefAsset.Layout == PageLayout.Table) {
                PopulateTitle(target);
                PopulateTable(target, target.RefAsset.TableData);
            }
        }

        public static void PopulateTitle(FlexPagePopulator target) {
            target.Page.Title.SetText(target.RefAsset.TextData.TitleText);
        }
        public static void PopulateTable(FlexPagePopulator target, TableData data) {
            ref FlexPage page = ref target.Page;
            int numChildren = page.Table.transform.childCount;
            for (int i = numChildren-1; i >= 0; i--) {
                Object.DestroyImmediate(page.Table.transform.GetChild(i).gameObject);
            }
            int cols = data.Headers.Length;
            page.Table.Grid.cellSize = new Vector2(page.Table.GetComponent<RectTransform>().rect.width / cols, 30);
            int rows = data.Cells.Length / cols;
            for (int r = -1; r < rows; r++) {
                for (int  c = 0; c < cols; c++) {
                    FlexCell newCell;
                    if (r == -1) {
                        newCell = Object.Instantiate(target.HeaderCell, page.Table.transform);
                        newCell.Text.SetText(data.Headers[c]);
                    } else if (data.ColumnHasImage[c]) {
                        newCell = Object.Instantiate(target.GraphicCell, page.Table.transform);
                        newCell.Image.sprite = data.Cells[r * cols + c].Image;
                    } else {
                        newCell = Object.Instantiate(target.TextCell, page.Table.transform);
                        newCell.Text.SetText(data.Cells[r * cols + c].Text);
                    }
                }
                GameObject rowBreak = Object.Instantiate(target.RowBreak, page.Table.transform);
                rowBreak.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30 * (r+2));
            }

        }

    }
}