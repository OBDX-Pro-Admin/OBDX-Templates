using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//namespace NVS_Flasher
//{
namespace MAUIExample.Templates
{
     
    public class NavItem
    {
        public NavItem(string title, string caption, string displayImage, bool isImageVisible)
        {
            Title = title;
            Caption = caption;
            DisplayImage = displayImage;
            IsImageVisible = isImageVisible;
        }

        public string Title { get; set; }
        public string Caption { get; set; }
        public string DisplayImage { get; set; }
        public bool IsImageVisible { get; set; }


    }

}
//}
