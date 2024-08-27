using EditorClasses;
using EditorClasses.Object;
using EngineClasses;
using Frame;
using Helpers;
using ModelClasses;
using ScadPluginLibrary.Interfaces.SCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Frame
{
    [ProgId("Frame.Lib")]
    [Guid("07F86D1F-53FB-4364-9FC8-AAAB5E1B1EC7")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]

    public class Main : IScad 
    {
        // Точка входа в пользовательское расширение. На вход программа SCAD 
        // передает программный интерфейс Engine. С помощью Engine вызываются
        //  объекты Settings, View, Model, Editor, Result.

        public void Run (dynamic engine)
        {
            try
            {
                Engine engineSCAD = new Engine(engine);
                Editor editor = engineSCAD.GetEditor();

                Control control = new Control(this, engineSCAD, editor);
            }
            finally
            {
                Marshal.ReleaseComObject(engine);
            }
        }
    }

    public class Control
    {
        const int xn = 0; const int yn = 1; const int zn = 2;
        public Main main;
        Engine engineSCAD;
        Editor editor;

        public string txt;
        public UInput input;
        private List<Nodes> UsedNodes = new List<Nodes>();

        public Control(Main main, Engine engineSCAD, Editor editor)
        {
            this.main = main;
            this.engineSCAD = engineSCAD;
            this.editor = editor;
            Start();
        }
        
        // Метод создает пользовательское окно в форме диалога. При нажатии в диалоге кнопки Ок запускается метод
        // построения одноэтажной или многоэтажной рамы каркаса здания в соответствии с данными введенными
        // пользователем. При нажатии кнопки Cancel плагин завершает свою работу. После вызова всех методов
        // освобождаются ресурсы системы вызомом метода Dispose().
        
        public void Start()
        {

            Form1 form1 = new Form1(this);
            DialogResult dr = form1.ShowDialog();
            if ( dr == DialogResult.OK)
            {
                switch (input.Mode)
                {
                    case "Single-story":
                    {
                        System.Windows.Forms.MessageBox.Show("Single-story mode");
                        CreateFrame(editor, input);
                        break;
                    }
                    case "Multi-storey":
                    {
                        System.Windows.Forms.MessageBox.Show("Multi-story mode");
                        CreateMultiStoreyFrame(editor, input);
                        break;
                    }
                    default:
                    {
                        System.Windows.Forms.MessageBox.Show("Mode input data is wrong");
                        break;
                    }
                }
            }
            else
            {
                engineSCAD.Cancel("Cancelled by user");
            }
            form1.Dispose();
        }

        // Метод создает одноэтажную раму каркаса здания. Методу передаются объекты Editor
        // и UInput. Editor отвечает за добавление и изменение исходных данных модели.
        // UInput содержит данные введенные пользователем.
        // Метод определяет координаты начала и конца элементов и вызывает метод CreateElem()
        // для добавления элементов в модель.

        public void CreateFrame(Editor editor, UInput input)
        {
            uint elemNum;
            List<double> firstNode = new List<double>() { 0, 0, 0 };
            List<double> secondNode = new List<double>() { 0, 0, input.Level };
            List<double> ridgeNode = new List<double>() { 0, 0, 0 };
            ridgeNode[xn] = secondNode[xn] + input.Span / 2;
            ridgeNode[zn] = secondNode[zn] + input.Span / 2 * input.Incline;
            JointEditor joint = new JointEditor
            {
                Place = 1,
                Mask = 48,
            };

            for (uint i = 0; i < input.SpanNum + 1; i++)
            {
                if (i != 0 && i != input.SpanNum)
                {
                    firstNode[xn] = firstNode[xn] + input.Span;
                    secondNode[xn] = secondNode[xn] + input.Span;
                    ridgeNode[xn] = secondNode[xn] + input.Span / 2;
                    ridgeNode[zn] = secondNode[zn] + input.Span / 2 * input.Incline;
                    elemNum = CreateElem(editor, firstNode, secondNode);
                    if (input.HingesOnGround) editor.JointSet(elemNum, 1, joint);
                    elemNum = CreateElem(editor, secondNode, ridgeNode, "roof");
                    if (input.HingesOnBeams) editor.JointSet(elemNum, 1, joint);
                    elemNum = CreateElem(editor, ridgeNode,
                        new List<double>() { secondNode[xn] + input.Span, 0, secondNode[zn] }, "roof");
                    if (input.HingesOnBeams) editor.JointSet(elemNum, 2, joint);
                }
                else if (i == 0)
                {
                    elemNum = CreateElem(editor, firstNode, secondNode, "first column");
                    if (input.HingesOnGround) editor.JointSet(elemNum, 1, joint);
                    elemNum = CreateElem(editor, secondNode, ridgeNode, "roof");
                    if (input.HingesOnBeams) editor.JointSet(elemNum, 1, joint);
                    elemNum = CreateElem(editor, ridgeNode,
                        new List<double>() { secondNode[xn] + input.Span, 0, secondNode[zn] }, "roof");
                    if (input.HingesOnBeams) editor.JointSet(elemNum, 2, joint);
                }
                else
                {
                    firstNode[xn] = firstNode[xn] + input.Span;
                    secondNode[xn] = secondNode[xn] + input.Span;
                    elemNum = CreateElem(editor, firstNode, secondNode, "last column");
                    if (input.HingesOnGround) editor.JointSet(elemNum, 1, joint);
                }
            }
        }

        // Метод создает многоэтажную раму каркаса здания. Методу передаются объекты Editor
        // и UInput. Editor отвечает за добавление и изменение исходных данных модели.
        // UInput содержит данные введенные пользователем.
        // Метод определяет координаты начала и конца элементов и вызывает метод CreateElem()
        // для добавления элементов в модель.

        public void CreateMultiStoreyFrame(Editor editor, UInput input)
        {
            uint elemNum;
            const int x = 0; const int y = 1; const int z = 2;
            List<double> firstNode = new List<double>() { 0, 0, 0 };
            List<double> secondNode = new List<double>() { 0, 0, input.Level };
            JointEditor joint = new JointEditor
            {
                Place = 1,
                Mask = 48,
            };

            for (int j = 0; j < input.StoreyNum; j++)
            {
                for (uint i = 0; i < input.SpanNum + 1; i++)
                {
                    if (i != 0 && i != input.SpanNum)
                    {
                        firstNode[x] = firstNode[x] + input.Span;
                        secondNode[x] = secondNode[x] + input.Span;
                        elemNum = CreateElem(editor, firstNode, secondNode);
                        if (input.HingesOnGround && j == 0) editor.JointSet(elemNum, 1, joint);
                        elemNum = CreateElem(editor, secondNode,
                            new List<double> { secondNode[x] + input.Span, 0, secondNode[z] }, "beam");
                        if (input.HingesOnBeams)
                        {
                            editor.JointSet(elemNum, 1, joint);
                            editor.JointSet(elemNum, 2, joint);
                        }
                    }
                    else if (i == 0)
                    {
                        elemNum = CreateElem(editor, firstNode, secondNode, "first column");
                        if (input.HingesOnGround && j == 0) editor.JointSet(elemNum, 1, joint);
                        elemNum = CreateElem(editor, secondNode,
                            new List<double>() { secondNode[x] + input.Span, 0, secondNode[z] }, "beam");
                        if (input.HingesOnBeams)
                        {
                            editor.JointSet(elemNum, 1, joint);
                            editor.JointSet(elemNum, 2, joint);
                        }
                    }
                    else
                    {
                        firstNode[x] = firstNode[x] + input.Span;
                        secondNode[x] = secondNode[x] + input.Span;
                        elemNum = CreateElem(editor, firstNode, secondNode, "last column");
                        if (input.HingesOnGround && j == 0) editor.JointSet(elemNum, 1, joint);
                    }
                }
                firstNode[x] = 0;
                secondNode[x] = 0;
                firstNode[z] = firstNode[z] + input.Level;
                secondNode[z] = secondNode[z] + input.Level;
            }
        }

        // Метод для поиска уже существующих узлов в модели для предотвращения
        // повторного создания узлов. Метод возвращает -1 если узел не найден и
        // номер узла если узел найден.

        public int FindNodes (List <Nodes> nodes, List<double> node)
        {
            if (UsedNodes == null)
            {
                return -1;
            }
            for (int i = 0; i < UsedNodes.Count; i++)
            {
                if (UsedNodes[i].NodeEditor.x == node[xn]
                    && UsedNodes[i].NodeEditor.y == node[yn]
                    && UsedNodes[i].NodeEditor.z == node[zn])
                {
                    return i;
                }
            }
            return -1;
        }

        // Метод создает елементы в модели Методу передаются объекты Editor, координаты начального и
        // конечного узлов и при необходимости имя создаваемого элемента.
        // Метод запускает поиск уже созданых узлов по координатам начального и конечного узлов. Если
        // узел найден переменной присваивается объект узла, если узел не найден он создается по указанным
        // координатам и добавляется в список с объектами узлов. После присвоения или создания узлов создается
        // елемент. Метод возвращает номер созданного элемента.

        public uint CreateElem(Editor editor, List<double> firstNode, List<double> secondNode, string name = "")
        {
            uint startNode = 0;
            uint endNode = 0;
            int findStartNode = FindNodes(UsedNodes, firstNode);
            int findEndNode = FindNodes(UsedNodes, secondNode);
            if (findStartNode > -1)
            {
                NodeEditor node_1 = UsedNodes[findStartNode].NodeEditor;
                startNode = UsedNodes[findStartNode].NodeNum;
                if (findEndNode > -1)
                {
                    NodeEditor node_2 = UsedNodes[findEndNode].NodeEditor;
                    endNode = UsedNodes[findEndNode].NodeNum;
                }
                else
                {
                    endNode = editor.NodeAdd(1);
                    NodeEditor node_2 = new NodeEditor()
                    { x = secondNode[xn], y = secondNode[yn], z = secondNode[zn] };
                    editor.NodeUpdate(endNode, node_2);
                    UsedNodes.Add(new Nodes(endNode, node_2));
                }
            }
            if (findStartNode < 0)
                {
                startNode = editor.NodeAdd(1);
                NodeEditor node_1 = new NodeEditor()
                { x = firstNode[xn], y = firstNode[yn], z = firstNode[zn] };
                editor.NodeUpdate(startNode, node_1);
                UsedNodes.Add(new Nodes(startNode, node_1));
                if (findEndNode > -1)
                {
                    NodeEditor node_2 = UsedNodes[findEndNode].NodeEditor;
                    endNode = UsedNodes[findEndNode].NodeNum;
                }
                else
                {
                    endNode = editor.NodeAdd(1);
                    NodeEditor node_2 = new NodeEditor()
                    { x = secondNode[xn], y = secondNode[yn], z = secondNode[zn] };
                    UsedNodes.Add(new Nodes(endNode, node_2));
                    editor.NodeUpdate(endNode, node_2);
                }
            }
            uint baseElemNum = editor.ElemAdd(1);
            ElemEditor elem = new ElemEditor()
            {
                Text = name,
                TypeElem = 5,
                ListNode = new object[] { startNode, endNode }
            };
            editor.ElemUpdate(baseElemNum, elem);
            return baseElemNum;
        }
        
        public string ListToString (List<double> list)
        {
            string str = "";
            foreach(double d in list)
            {
                str += d.ToString() + " - ";
            }
            return str;
        }
    }

    // Класс для хранения объекта узла и его номера в модели.
    // Номер узла в модели и номер в списке могут не совпадать.

    public class Nodes
    {
        private uint nodeNum;
        public uint NodeNum
        {
            get { return nodeNum; }
            set { nodeNum = value; }
        }
        private NodeEditor nodeEditor;
        public NodeEditor NodeEditor
        {
            get { return nodeEditor; }
            set { nodeEditor = value; }
        }

        public Nodes (uint nodeNum, NodeEditor nodeEditor)
        {
            this.nodeNum = nodeNum;
            this.nodeEditor = nodeEditor;
        }
    }
    
    // Класс для хранения объекта содержащего данные введенные пользователем

    public class UInput
    {
        private string mode;
        public string Mode
        {
            get { return mode; }
            set { mode = value; }
        }
        
        private double span;
        public double Span
        {
            get { return span; }
            set { span = value; }
        }
        private uint spanNum;
        public uint SpanNum
        {
            get { return spanNum; }
            set { spanNum = value; }
        }
        private double level;
        public double Level
        {
            get { return level ; }
            set { level = value; }
        }
        private double incline;
        public double Incline
        {
            get { return incline; }
            set { incline = value; }
        }
        private uint storeyNum;
        public uint StoreyNum
        {
            get { return storeyNum; }
            set { storeyNum = value; }
        }
        private bool hingesOnGround;
        public bool HingesOnGround
        {
            get { return hingesOnGround; }
            set { hingesOnGround = value; }
        }
        private bool hingesOnBeams;
        public bool HingesOnBeams
        {
            get { return hingesOnBeams; }
            set { hingesOnBeams = value; }
        }

        public UInput() 
        {

        }
        public UInput (double span, uint spanNum, double level, double incline)
        {
            this.span = span;
            this.spanNum = spanNum;
            this.level = level;
            this.incline = incline;
        }

        public override string ToString()
        {
            string str = "Mode: " + this.mode + "| Span: " + this.span +
                "| SpanNum: " + this.spanNum + "| level: " + this.level +
                "| Incline: " + this.incline +  "| Number of storeys: "+ this.storeyNum +
                "| Hinges on Ground: " + this.hingesOnGround +
                "| Hinges on Beams: " + this.hingesOnBeams;
            return str;
        }
    }

    [Guid("83C24F18-36EE-4F2F-B96E-480074BE0EAB")]//Сгенерировать свой
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [ComVisible(true)]
    public interface IScad
    {
        [DispId(1)]
        void Run(dynamic engine);
    }
}
