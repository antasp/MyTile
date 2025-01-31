﻿using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MyTile
{
    public partial class Form1 : Form
    {
        List<String> startmenuEntries;
        public Form1()
        {
            InitializeComponent();
            startmenuEntries = new List<String>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            //C:\ProgramData\Microsoft\Windows\Start Menu\Programs
            string[] potentialFolders = { Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs" };
            foreach (string startmenufolder in potentialFolders) {
                foreach (string filename in Directory.GetFiles(startmenufolder, "*.lnk", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(filename) == ".lnk")
                    {
                        startmenuEntries.Add(filename);
                    }
                }
            }
            lstStartmenu.Items.AddRange(startmenuEntries.ToArray());
        }

        private void lstStartmenu_SelectedIndexChanged(object sender, EventArgs e)
        {
            WshShell shell = new WshShell(); //Create a new WshShell Interface
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(lstStartmenu.SelectedItem.ToString()); //Link the interface to our shortcut

            string targetExe = link.TargetPath;
            if (!System.IO.File.Exists(targetExe))
            {
                //the shortcut points to something invalid...
                targetExe = targetExe.Replace(" (x86)", "");
                if(!System.IO.File.Exists(targetExe)){
                    MessageBox.Show("Can not find the target this shortcut is pointing to. Please enter application target manually.");
                    return;
                }
            }

            txtTarget.Text = targetExe; //Show the target in a MessageBox using IWshShortcut

            String targetTemplate = getTemplatePath(targetExe);
            txtImageFile.Text = "";
            txtImageSmall.Text = "";
            txtTilename.Text = "";
            txtBg.Text = "";
            txtSmallTilename.Text = "";
            pictureBox1.ImageLocation = "";
            pictureBox2.ImageLocation = "";

            if (System.IO.File.Exists(targetTemplate))
            {
                //we already have a template here!
                string tilename = getTileNameFromXML(targetExe, targetTemplate);
                string smalltilename = getSmallTileNameFromXML(targetExe, targetTemplate);
                string tilecolor = getTileColorFromXML(targetExe, targetTemplate);
                string textcolor = getTextColorFromXML(targetExe, targetTemplate);
                string text = getTextStateFromXML(targetExe, targetTemplate);


                string tilePath = Path.GetDirectoryName(targetExe) + @"\" + tilename;
                string smalltilePath = Path.GetDirectoryName(targetExe) + @"\" + smalltilename;

                txtBg.Text = tilecolor;

                if (text == "off")
                {
                    checkShowlabel.Checked = false;
                }
                else if(text == "on")
                {
                    checkShowlabel.Checked = true;
                }

                if(textcolor == "dark")
                {
                    radioDark.Checked = true;
                    radioLight.Checked = false;
                }
                else if(textcolor == "light")
                {
                    radioDark.Checked = false;
                    radioLight.Checked = true;
                }
                   
                if (!tilename.Equals("") && System.IO.File.Exists(tilePath))
                {
                    pictureBox1.ImageLocation = tilePath;
                    txtImageFile.Text = tilePath;
                    txtTilename.Text = tilename;
                }

                if (!smalltilename.Equals("") && System.IO.File.Exists(smalltilePath))
                {
                    pictureBox2.ImageLocation = smalltilePath;
                    if (!tilePath.Equals(smalltilePath))
                    {
                        txtImageSmall.Text = smalltilePath;
                        txtSmallTilename.Text = smalltilename;
                    }
                }

                
                
            }
        }

        private void btnBrowseImage_Click(object sender, EventArgs e)
        {
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            if (userClickedOK != DialogResult.OK) return;

            if (System.IO.File.Exists(openFileDialog1.FileName))
            {
                txtImageFile.Text = openFileDialog1.FileName;
                pictureBox1.ImageLocation = txtImageFile.Text;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string sourceTile = txtImageFile.Text;
            if (sourceTile.Length > 0 && !System.IO.File.Exists(sourceTile))
            {
                MessageBox.Show("No TileImage selected.");
                return;
            }
            string targetExe = txtTarget.Text;
            if (!System.IO.File.Exists(targetExe))
            {
                MessageBox.Show("No Target selected.");
                return;
            }

            string sourceTileSmall = txtImageSmall.Text;
            if (sourceTileSmall.Length > 0)
            {
                if (!System.IO.File.Exists(sourceTileSmall))
                {
                    MessageBox.Show("Small Tile specified but does not exist.");
                    return;
                }
            }

            if (sourceTile.Length > 0)
            {

                if (txtTilename.Text.Equals("") || txtTilename.Text.Equals("tile.png"))
                {
                    txtTilename.Text = getDefaultTilename(targetExe);
                }

                //copy the image.
                string targetTile = Path.GetDirectoryName(targetExe) + @"\" + txtTilename.Text;
                if (!sourceTile.Equals(targetTile))
                {
                    System.IO.File.Copy(sourceTile, targetTile, true);
                }
            }

            if (sourceTileSmall.Length > 0)
            {
                if (sourceTileSmall.Length > 0 && (txtSmallTilename.Text.Equals("") || txtSmallTilename.Text.Equals("tile-small.png")))
                {
                    txtSmallTilename.Text = getDefaultSmalltileName(targetExe);
                }


                if (sourceTileSmall.Length > 0)
                {
                    //copy the image.
                    string targetTileSmall = Path.GetDirectoryName(targetExe) + @"\" + txtSmallTilename.Text;
                    if (!sourceTileSmall.Equals(targetTileSmall))
                        System.IO.File.Copy(sourceTileSmall, targetTileSmall, true);
                }

            }

            XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

            XDocument doc =
             new XDocument(
               new XElement("Application", new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                 new XElement("VisualElements",
                    new XAttribute("ShowNameOnSquare150x150Logo", checkShowlabel.Checked ? "on" : "off"),
                    new XAttribute("ForegroundText", radioDark.Checked ? "dark" : "light"),
                    new XAttribute("BackgroundColor", txtBg.Text)
                    )
               )
             );

            var ve = doc.Element("Application").Element("VisualElements");

            if (sourceTile.Length > 0)
            {
                ve.Add(new XAttribute("Square150x150Logo", txtTilename.Text));

                if (!(sourceTileSmall.Length > 0))
                {
                    ve.Add(new XAttribute("Square70x70Logo", txtTilename.Text));
                }
                
            }
            
            if (sourceTileSmall.Length > 0)
            {
                ve.Add(new XAttribute("Square70x70Logo", txtSmallTilename.Text));
            }

            string targetTemplate = getTemplatePath(targetExe);
            doc.Save(targetTemplate);

            string lnkFile = lstStartmenu.SelectedItem.ToString();
            refreshLnk(lnkFile);
        }

        private string getTemplatePath(String exePath)
        {
            string targetTemplate = Path.GetDirectoryName(exePath) + @"\" + Path.GetFileNameWithoutExtension(exePath) + @".visualelementsmanifest.xml";
            return targetTemplate;
        }

        private void refreshLnk(String lnkFile)
        {
            System.IO.File.SetCreationTime(lnkFile, DateTime.Now);
            System.IO.File.SetLastAccessTime(lnkFile, DateTime.Now);
            System.IO.File.SetLastWriteTime(lnkFile, DateTime.Now);

            lstStartmenu_SelectedIndexChanged(null, null);
        }

        private void txtBg_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void txtBg_DoubleClick(object sender, EventArgs e)
        {
            DialogResult res = colorDialog1.ShowDialog();

            if(res != DialogResult.OK)
            {
                return;
            }

            txtBg.Text = System.Drawing.ColorTranslator.ToHtml(colorDialog1.Color);

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            string targetExe = txtTarget.Text;
            if (!System.IO.File.Exists(targetExe))
            {
                MessageBox.Show("No Target selected.");
                return;
            }

            string targetTemplate = getTemplatePath(targetExe);
            //string targetTile = getTilePathFromXML(targetExe, targetTemplate);
            //string targetTileSmall = getSmallTilePathFromXML(targetExe, targetTemplate);

            System.IO.File.Delete(targetTemplate);
            //System.IO.File.Delete(targetTile);
            //System.IO.File.Delete(targetTileSmall);

            string lnkFile = lstStartmenu.SelectedItem.ToString();
            refreshLnk(lnkFile);
        }

        private String getDefaultSmalltileName(String targetExe)
        {
            return Path.GetFileNameWithoutExtension(targetExe) + "-mytile-small.png";
        }

        private string getDefaultTilename(String targetExe)
        {
            return Path.GetFileNameWithoutExtension(targetExe) + "-mytile.png";
        }

        private String getTileNameFromXML(String targetExe, String templatePath)
        {
            XDocument targetXml = XDocument.Load(templatePath);
            XElement ve = targetXml.Element("Application").Element("VisualElements");
            var attribute = ve.Attribute("Square150x150Logo");
            return attribute != null ? attribute.Value : "";
        }

        private String getSmallTileNameFromXML(String targetExe, String templatePath)
        {
            XDocument targetXml = XDocument.Load(templatePath);
            XElement ve = targetXml.Element("Application").Element("VisualElements");
            var attribute = ve.Attribute("Square70x70Logo");
            return attribute != null ? attribute.Value : "";
        }

        private String getTileColorFromXML(String targetExe, String templatePath)
        {
            XDocument targetXml = XDocument.Load(templatePath);
            XElement ve = targetXml.Element("Application").Element("VisualElements");
            var attribute = ve.Attribute("BackgroundColor");
            return attribute != null ? attribute.Value : "";
        }
        private String getTextColorFromXML(String targetExe, String templatePath)
        {
            XDocument targetXml = XDocument.Load(templatePath);
            XElement ve = targetXml.Element("Application").Element("VisualElements");
            var attribute = ve.Attribute("ForegroundText");
            return attribute != null ? attribute.Value : "";
        }

        private String getTextStateFromXML(String targetExe, String templatePath)
        {
            XDocument targetXml = XDocument.Load(templatePath);
            XElement ve = targetXml.Element("Application").Element("VisualElements");
            var attribute = ve.Attribute("ShowNameOnSquare150x150Logo");
            return attribute != null ? attribute.Value : "";
        }

        private String getTilePathFromXML(String targetExe, String templatePath)
        {
            String tilePathCandidate = Path.GetDirectoryName(targetExe) + @"\tile.png";
            if (System.IO.File.Exists(tilePathCandidate))
                return tilePathCandidate;
            return "";
        }

        private String getSmallTilePathFromXML(String targetExe, String templatePath)
        {
            String tilePathCandidate = Path.GetDirectoryName(targetExe) + @"\tile-small.png";
            if (System.IO.File.Exists(tilePathCandidate))
                return tilePathCandidate;
            return "";
        }

        private void btnSmallImage_Click(object sender, EventArgs e)
        {
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            if (userClickedOK != DialogResult.OK) return;

            if (System.IO.File.Exists(openFileDialog1.FileName))
            {
                txtImageSmall.Text = openFileDialog1.FileName;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            string lnkFile = lstStartmenu.SelectedItem.ToString();
            if(System.IO.File.Exists(lnkFile))
                refreshLnk(lnkFile);
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            lstStartmenu.BeginUpdate();
            lstStartmenu.Items.Clear();

            lstStartmenu.Items.AddRange(startmenuEntries.Where(i => i.ToLower().Contains(txtFilter.Text.ToLower())).ToArray());

            lstStartmenu.EndUpdate();
        }

        private void resetColor_Click(object sender, EventArgs e)
        {
            txtBg.Text = "#404040";
        }
    }
}
