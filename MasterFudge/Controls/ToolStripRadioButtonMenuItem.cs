using System;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace MasterFudge.Controls
{
    public class ToolStripRadioButtonMenuItem : ToolStripMenuItem, IBindableComponent
    {
        public ToolStripRadioButtonMenuItem() : base() { Initialize(); }
        public ToolStripRadioButtonMenuItem(string text) : base(text) { Initialize(); }
        public ToolStripRadioButtonMenuItem(Image image) : base(image) { Initialize(); }
        public ToolStripRadioButtonMenuItem(string text, Image image) : base(text, image) { Initialize(); }
        public ToolStripRadioButtonMenuItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) { Initialize(); }
        public ToolStripRadioButtonMenuItem(string text, Image image, params ToolStripMenuItem[] dropDownItems) : base(text, image, dropDownItems) { }
        public ToolStripRadioButtonMenuItem(string text, Image image, EventHandler onClick, Keys shortcutKeys) : base(text, image, onClick, shortcutKeys) { Initialize(); }
        public ToolStripRadioButtonMenuItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { Initialize(); }

        BindingContext bindingContext;
        ControlBindingsCollection dataBindings;

        public BindingContext BindingContext
        {
            get
            {
                if (bindingContext == null)
                    bindingContext = new BindingContext();
                return bindingContext;
            }
            set
            {
                bindingContext = value;
            }
        }

        public ControlBindingsCollection DataBindings
        {
            get
            {
                if (dataBindings == null)
                    dataBindings = new ControlBindingsCollection(this);
                return dataBindings;
            }
        }

        public override bool Enabled
        {
            get
            {
                ToolStripMenuItem ownerMenuItem = OwnerItem as ToolStripMenuItem;

                if (!DesignMode && ownerMenuItem != null && ownerMenuItem.CheckOnClick)
                    return base.Enabled && ownerMenuItem.Checked;
                else
                    return base.Enabled;
            }
            set
            {
                base.Enabled = value;
            }
        }

        public void Initialize()
        {
            CheckOnClick = true;
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);

            if (!Checked || Parent == null) return;

            foreach (ToolStripItem item in Parent.Items)
            {
                ToolStripRadioButtonMenuItem radioItem = item as ToolStripRadioButtonMenuItem;
                if (radioItem != null && radioItem != this && radioItem.Checked)
                {
                    radioItem.Checked = false;
                    return;
                }
            }
        }

        protected override void OnClick(EventArgs e)
        {
            if (Checked) return;
            base.OnClick(e);
        }
    }
}
