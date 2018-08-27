using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoCecilPatcher.Properties;

namespace MonoCecilPatcher
{
    public partial class Form1 : Form
    {
        public static bool saveAssemblyValues = false;
        public static bool saveDllValues = false;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "dll files (*.dll) | *.dll";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox5.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Textbox not empty check.
            foreach (Control x in this.Controls)
            {
                if (x is TextBox)
                {
                    if (string.IsNullOrWhiteSpace(x.Text))
                    {
                        MessageBox.Show("The field " + x.Tag + " is empty,\nplease fill it.", "Empty field",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            string dllPath = textBox5.Text;
            string assemblyPath = textBox9.Text;
            string dllClassName = textBox10.Text;
            string dllMethodName = textBox6.Text;
            string assemblyClassName = textBox7.Text;
            string assemblyMethodName = textBox8.Text;

            //Because often assemblies are linked, we move in the assembly directory to avoid error file not found.
            Directory.SetCurrentDirectory(Path.GetDirectoryName(assemblyPath));

            //Because if you launched the patched version without the dll containing the code in the same directory
            //it won't work, we check that the dll is in the directory and if not we copy it in.
            string dllInAssemblyDirectory = Path.GetDirectoryName(assemblyPath) + "\\" + Path.GetFileName(dllPath);
            File.Copy(dllPath, dllInAssemblyDirectory, true);
            

            //Getting module.
            var injassembly = AssemblyDefinition.ReadAssembly(dllPath);

            //Setting module method that will replace the assembly one.
            var typedefinj = injassembly.MainModule.Types.Single(t => t.Name == dllClassName);
            var injmethDefinition = typedefinj.Methods.Single(t => t.Name == dllMethodName);

            //Getting assembly.
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            //Setting assembly method that will be replaced by the module one.
            var typeDefinition = assembly.MainModule.Types.Single(t => t.Name == assemblyClassName);
            var methDefinition = typeDefinition.Methods.Single(t => t.Name == assemblyMethodName);
            
            var setMethodWriter = methDefinition.Body.GetILProcessor();
            var firstExistingInstruction = setMethodWriter.Body.Instructions[0];

            //Injecting in assembly method beginning the module method.
            setMethodWriter.InsertBefore(firstExistingInstruction, setMethodWriter.Create(OpCodes.Call, assembly.MainModule.Import(injmethDefinition.Resolve())));

            string patchPath = "";
            switch (Path.GetExtension(assemblyPath))
            {
                case ".exe":
                    //If it is an executable we replace it directly with a new one name-patched.exe
                    patchPath = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileNameWithoutExtension(assemblyPath)) + "-patched" + Path.GetExtension(assemblyPath);
                    break;
                case ".dll":
                    //Else we need to replace the.dll and backup the original one naming it name.orig.dll
                    patchPath = assemblyPath;
                    break;
            }


            try
            {
                assembly.Write(patchPath);
                if(MessageBox.Show("Assembly sucessfully patched, you can find it here :\n" + patchPath, "Patch done !",
                        MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(assemblyPath));
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Something went wrong, here is the exception caught :\n" + ex.ToString(), "Error patching",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox9.Text = openFileDialog1.FileName;
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            saveAssemblyValues = !saveAssemblyValues;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (saveAssemblyValues)
            {
                Settings.Default.assemblyPath = textBox9.Text;
                Settings.Default.assemblyClassName = textBox7.Text;
                Settings.Default.assemblyMethodName = textBox8.Text; 
            }
            if (saveDllValues)
            {
                Settings.Default.dllPath = textBox5.Text;
                Settings.Default.dllClassName = textBox10.Text;
                Settings.Default.dllMethodName = textBox6.Text;
            }

            Settings.Default.rememberAssembly = checkBox3.Checked;
            Settings.Default.rememberDll = checkBox1.Checked;
            Settings.Default.Save();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            saveDllValues = !saveDllValues;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Settings.Default.dllPath != null)
            {
                textBox5.Text = Settings.Default.dllPath;
                textBox10.Text = Settings.Default.dllClassName;
                textBox6.Text = Settings.Default.dllMethodName;
                checkBox1.Checked = Settings.Default.rememberDll;
            }
            if(Settings.Default.assemblyPath != null)
            {
                textBox9.Text = Settings.Default.assemblyPath;
                textBox7.Text = Settings.Default.assemblyClassName;
                textBox8.Text = Settings.Default.assemblyMethodName;
                checkBox3.Checked = Settings.Default.rememberAssembly;
            }
        }
    }
}
