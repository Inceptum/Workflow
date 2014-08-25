namespace Inceptum.Workflow
{
    public class ActivityState
    {
        public dynamic Values { get; set; }
        public string NodeName { get; set; }
        public ActivityResult Status { get; set; }

        public override string ToString()
        {
            return string.Format("ActivityState for node name {0} status {1}", NodeName, Status);
        }
    }
}