using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//namespace NVS_Flasher
//{
namespace MAUIExample1.Templates
{

    public class NewsItem
    {

        public NewsItem(DateTime _date, string _caption, string _body)
        {
            Date = _date;
            Caption = _caption;
            Body = _body;
        }

        // public string Id { get; set; }
        public DateTime Date { get; set; }
        public string Caption { get; set; }
        public string Body { get; set; }

    }


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
