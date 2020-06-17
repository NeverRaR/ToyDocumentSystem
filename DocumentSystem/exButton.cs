using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Threading;
using System.Collections;

namespace DocumentSystem
{
    class exButton: Button
    {
        public delegate void SingleClickEventHandler(String s);
        public delegate void DoubleClickEventHandler();
        public new event DoubleClickEventHandler DoubleClick;
        public event SingleClickEventHandler SingleClick;

        DateTime clickTime;
        bool isClicked = false;
        public string name;
      
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            if (isClicked)
            {
                TimeSpan span = DateTime.Now - clickTime;
                if (span.Milliseconds < SystemInformation.DoubleClickTime)
                {
                    DoubleClick();
                }
                isClicked = false;
            }
            else
            {
                isClicked = true;
                clickTime = DateTime.Now;
                SingleClick(name);
            }
        }
    }
}
