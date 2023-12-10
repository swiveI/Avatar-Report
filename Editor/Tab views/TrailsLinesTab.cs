using UnityEditor;
using UnityEngine;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class TrailsLinesTab : AvatarReportTab
    {
        public override void OnTabOpen()
        {
            base.OnTabOpen();
        }

        public override void OnTabClose()
        {
            base.OnTabClose();
        }

        public override void OnTabGui(float offset)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("This tab has not been implemented yet :c", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }
        }

        public override void OnAvatarChanged()
        {
            base.OnAvatarChanged();
        }
    }
}