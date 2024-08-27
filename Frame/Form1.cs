using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Frame
{
    // Класс для постраения диалогового окна для ввода пользователем данных,
    // их валидации и передачи для выполнения.
    // Диалоговое окно состоит из двух закладок для двух типов исходных данных
    // создания одноэтажной рамы здания или многоэтажной рамы.

    public partial class Form1 : Form
    {
        UInput input = new UInput();
        public Control control;

        public Form1(Control control)
        {
            this.control = control;
            
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            
        }

        // Метод проверяет данные введенные пользователем в объекте TextBox. Данные должны
        // быть десятичным числом больше нуля. Одно из значений может быть меньше нуля.

        private bool CheckValue(TextBox TB, string str, bool skipZero = false)
        {
            string fixedStr = str.Replace('.', ',');
            TB.Text = fixedStr;

            if (!(Double.TryParse(fixedStr, out double value) && (value > 0 || skipZero)))
            {
                MessageBox.Show($"Value {fixedStr} must be a number or above 0", TB.Name, MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        // Метод проверяет данные введенные пользователем в объекте TextBox. Данные должны
        // быть целым числом больше нуля.

        private void CheckIntValue(TextBox TB, string str)
        {
            if (!(UInt32.TryParse(str, out uint value) && value > 0 ))
            {
                MessageBox.Show($"Value {str} must be a integer number above 0", TB.Name, MessageBoxButtons.OK);
            }
        }

        // Обработчик события, вызываемый при переходе пользователя к другому элементу
        // диалогового окна. Вызывает проверку для целого числа, десятичного больше нуля
        // и десятичного допускающего значение ниже нуля.

        private void text_Box_Leave(object sender, EventArgs e)
        {
            TextBox TB = (TextBox)sender;
            switch (TB.Tag)
            {
                case "skipZero":
                    CheckValue(TB, TB.Text, true);
                    break;
                case "integer":
                    CheckIntValue(TB, TB.Text);
                    break;
                default:
                    CheckValue(TB, TB.Text);
                    break;
            }
        }

        // Обработчики события для нажатия клавиши Cancel. При нажатии скрывают окно
        // и присвоивают переменной DialogResult значение Cancel.

        private void Cancel_but_MouseUp(object sender, MouseEventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
        private void Cancel_but2_MouseUp(object sender, MouseEventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        // Обработчики событий выбора радиокнопок

        private void hingesOnGroundYes_CheckedChanged(object sender, EventArgs e) { }
        private void hingesOnGroundNo_CheckedChanged(object sender, EventArgs e) { }
        private void hingesOnBeamsNo_CheckedChanged(object sender, EventArgs e) { }
        private void hingesOnBeamsYes_CheckedChanged(object sender, EventArgs e) { }

        // Обработчики события нажатия кнопки Ок в закладках. Если одно из значений
        // в объекте TextBox не соответствует ожидаемому то перехватывается исключение
        // System.FormatException и пользователю показывается подсказка с нужными форматами данных.

        private void OK_but_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                input.Mode = "Single-story";
                input.Span = Double.Parse(span_Box.Text);
                input.SpanNum = Convert.ToUInt16(spanNum_Box.Text);
                input.Level = Double.Parse(height_Box.Text);
                input.Incline = Double.Parse(inclination_Box.Text);
                input.HingesOnGround = hingesOnGroundYes.Checked;
                input.HingesOnBeams = hingesOnBeamsYes.Checked;

                control.input = input;
                this.DialogResult = DialogResult.OK;
            }
            catch (System.FormatException ex)
            {
                MessageBox.Show("Values of Span, Heigth and Inclination must be decimal number.\n" +
                    "Values of Number of spans must be integer.");
            }
        }

        private void OK_but2_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                input.Mode = "Multi-storey";
                input.Span = Double.Parse(tap2_span_Box.Text);
                input.SpanNum = Convert.ToUInt16(tap2_spanNum_Box.Text);
                input.Level = Double.Parse(tap2_height_Box.Text);
                input.StoreyNum = (uint)Convert.ToInt16(tap2_storeysNum_Box.Text);
                input.HingesOnGround = tap2_hingesOnGroundYes.Checked;
                input.HingesOnBeams = tap2_hingesOnBeamsYes.Checked;
                control.input = input;
                MessageBox.Show(input.ToString());
                this.DialogResult = DialogResult.OK;
            }
            catch (System.FormatException ex) {
                MessageBox.Show("Values of Span and Heigth must be decimal number.\n" +
                    "Values of Number of spans and Number of storeys must be integer.");
            }
        }
    }
}
