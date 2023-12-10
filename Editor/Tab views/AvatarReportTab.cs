using UnityEditor;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public abstract class AvatarReportTab //: EditorWindow
    {
        public bool Initialized = false;
        public virtual void OnTabOpen() {}
        public virtual void OnTabClose() {}
        public virtual void OnTabGui(float offset) { }
        public virtual void OnAvatarChanged() { }
    }
}
