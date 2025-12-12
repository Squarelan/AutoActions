using System.Drawing;

namespace AutoActions
{
    public interface IApplicationItem
    {
        string ApplicationFilePath { get; set; }
        string ApplicationName { get; set; }
        string DisplayName { get; set; }
        Bitmap Icon { get; set; }

        void Restart();
        void StartApplication();
        string ToString();



    }
}