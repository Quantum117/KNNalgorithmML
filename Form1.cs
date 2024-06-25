using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;

using DataTable = Microsoft.Office.Interop.Excel.DataTable;

namespace WindowsFormsApp1
{

    public partial class Form1 : Form
    {

        private string fileName;
        private int k;
        private List<double> testValues = new List<double>();
        private HashSet<string> uniqueClasses = new HashSet<string>();

        public Form1()
        {
            InitializeComponent();
        }
        private double ParseToDouble(string value)
        {
            var culture = CultureInfo.InvariantCulture; // Use InvariantCulture for a consistent interpretation of decimal separators
            if (double.TryParse(value, NumberStyles.Any, culture, out double result))
            {
                return result;
            }
            else
            {
                throw new FormatException($"Unable to parse '{value}' as double.");
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            k = Convert.ToInt32(numericUpDown1.Value);

            using (OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Text Files|*.txt" })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = openFileDialog.FileName;
                    toolStripStatusLabel1.Text = "Text file loaded.";
                    LoadDataFromFile(fileName);
                }
            }
        }

        private void LoadDataFromFile(string filePath)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                bool isFirstLine = true;

                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(' ');

                    if (isFirstLine)
                    {
                        // Create columns for the DataTable
                        for (int i = 0; i < parts.Length; i++)
                        {
                            dataTable.Columns.Add($"Column{i + 1}", typeof(string));
                        }
                        isFirstLine = false;
                    }

                    var newRow = dataTable.NewRow();
                    for (int i = 0; i < parts.Length; i++)
                    {
                        newRow[i] = parts[i];
                    }

                    dataTable.Rows.Add(newRow);
                }
            }

            dataGridView1.DataSource = dataTable;
        }

        private double CalculateDistance(double[] testData, double[] dataPoint)
        {
            double distance = 0;
            for (int i = 0; i < testData.Length; i++)
            {
                distance += Math.Pow((testData[i] - dataPoint[i]), 2);
            }
          distance = Math.Sqrt(distance);
            return distance;
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string testDataString = textBoxTestData.Text;
            var testData = testDataString.Split(' ').Select(double.Parse).ToArray();

            if (testData.Length != 4)
            {
                MessageBox.Show("Invalid test data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
           
            string s="";
            foreach(var i in testData )
            {
                s += i + " ";
            }
            MessageBox.Show(s);

            if (testData.Length != 4) // Assumes 4 attributes in test data
            {
                MessageBox.Show("Invalid test data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get data from the DataGridView
            var dataPoints = new List<double[]>();
            var classData = new List<string>();

            for (int i = 0; i < dataGridView1.RowCount - 1; i++)
            {
                double[] dataRow = new double[4];
                for (int j = 0; j < 4; j++)
                {
                    dataRow[j] = Convert.ToDouble(dataGridView1.Rows[i].Cells[j+1].Value);
                }
                dataPoints.Add(dataRow);
                classData.Add(dataGridView1.Rows[i].Cells[0].Value.ToString());
            }

            // Calculate distances from the test data to each point in the dataset
            var distances = dataPoints
                .Select((point, index) => new
                {
                    Index = index,
                    Distance = CalculateDistance(testData, point),
                    Class = classData[index]
                })
                .OrderBy(item => item.Distance)
                .Take(k) // Only take the top k distances
                .ToArray();

            // Display distances in the rich text boxes
            richTextBoxDistance.Clear();
            richTextBoxIndex.Clear();
            richTextBoxClass.Clear();

            foreach (var item in distances)
            {
                richTextBoxDistance.AppendText(item.Distance.ToString() + Environment.NewLine);
                richTextBoxIndex.AppendText(item.Index.ToString() + Environment.NewLine);
                richTextBoxClass.AppendText(item.Class + Environment.NewLine);
            }

            // Determine the most frequent class in the top k
            var mostFrequentClass = distances
                .GroupBy(d => d.Class)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            toolStripStatusLabel1.Text = $"The class of test data is: {mostFrequentClass}";

            if (k % 2 == 0)
            {
                MessageBox.Show("Value of K must be odd.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (distances.Count(d => d.Class == "Class1") > k / 2)
            {
                MessageBox.Show("Too many elements from Class1.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (distances.Count(d => d.Class == "Class2") > k / 2)
            {
                MessageBox.Show("Too many elements from Class2.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Handle cell content click events here
        }
    }
}