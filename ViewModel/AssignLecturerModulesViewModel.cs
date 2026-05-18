namespace SchoolProject.ViewModel
{
    public class AssignLecturerModulesViewModel
    {
        public int UserID { get; set; }
        public List<int> SelectedModuleIDs { get; set; } = new List<int>();
        public DateTime AssignedDate { get; set; } = DateTime.Now;
    }
}