using UnityEngine;


namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public class TestAvRTab : AvatarReportTab
    {
        public override void OnTabOpen()
        {
            Debug.Log("TestAvRTab opened");
        }
        
        public override void OnTabGui(float offset)
        {
            GUILayout.Label("This is a test tab");
        }

        public override void OnAvatarChanged()
        {
            Debug.Log("TestAvRTab avatar changed");
        }
    }   
}