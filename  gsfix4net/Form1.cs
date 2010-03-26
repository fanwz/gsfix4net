using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QuickFix;
using QuickFix42;
using System.Collections;
using System.Threading;
namespace QuickFixInitiator
{
    public partial class Form1 : Form
    {
        QuickFixWrapper _quickFixWrapper = new QuickFixWrapper();
        bool isconnected = false;
        private Dictionary<string, OrderInfo> ordersAll=new Dictionary<string,OrderInfo> ();
        private int fClOrdID;
        //ClOrdID  lastclordid=null  ;
        //Symbol lastsymbol = null;
        //Side lastside = null;
        public Form1()
        {
            InitializeComponent();
            //listBox1.Items.Add("�����ʻ�");
            //listBox1.Items.Add("�ʽ��ʻ�");
            _quickFixWrapper.OnError += new EventHandler(_quickFixWrapper_OnError);
            _quickFixWrapper.OnConnection += new EventHandler(_quickFixWrapper_OnConnection);
            _quickFixWrapper.OnDisconnection += new EventHandler(_quickFixWrapper_OnDisconnection);
            _quickFixWrapper.ExecutionReport += new ExecutionReportEventHandler(_quickFixWrapper_ExecutionReport);
            _quickFixWrapper.OrderCancelReject += new OrderCancelRejectEventHandler(_quickFixWrapper_OrderCancelReject);
        }

        void _quickFixWrapper_OnError(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            //string msg = (string)sender;
            //MessageBox.Show(msg);
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(_quickFixWrapper_OnError), new object[] { sender, e });
            }
            else
            {
                string msg = (string)sender;
                label16.Text = "FIX����->" + msg;
                dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "ע��",msg);
            }
            //dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "����");
        }
        /// <summary>
        /// ���еĳ����ܾ����ľ���GUI����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _quickFixWrapper_OrderCancelReject(object sender, OrderCancelRejectEventArgs args)
        {
            //throw new Exception("The method or operation is not implemented.");
            if (this.InvokeRequired)
            {
                
                this.Invoke(new OrderCancelRejectEventHandler(_quickFixWrapper_OrderCancelReject), new object[] { sender, args });
            }
            else
            {
                string msg = args.OrderCancelReject.ToString();
                //label4.Text = "FIX����->" + msg;
                dataGridView1.Rows.Insert(0, DateTime.Now.ToString(),"FIX", msg);
                dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "ҵ��", "�����ܾ���" + args.OrderCancelReject.getOrigClOrdID());
            }
           
        }
        /// <summary>
        /// ���еĳɽ��ر����ر�����ο�FIXЭ�飩���ľ���GUI���ԡ�����new��replace�ر����Ǽǲ���ָ����Ϣ���������ڳ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _quickFixWrapper_ExecutionReport(object sender, ExecutionReportEventArgs args)
        {
            string key = null;
            OrderInfo info;
            ExecutionReport executionReport = args.ExecutionReport;

            char exec = executionReport.getExecType().getValue();
            if (exec == ExecType.PENDING_CANCEL || exec == ExecType.CANCELED || exec == ExecType.PENDING_REPLACE || exec == ExecType.REPLACE)
            {
                key = executionReport.getOrigClOrdID().getValue();
            }
            else
            {
                key = executionReport.getClOrdID().getValue();
            }         //throw new Exception("The method or operation is not implemented.");
            if (this.InvokeRequired)
            {
                try
                {
                    if (ordersAll.ContainsKey(key))
                    {
                        info = ordersAll[key];
                        if (!args.ExecutionReport.isSetTransactTime())
                        {
                            args.ExecutionReport.setString(60, DateTime.Now.ToUniversalTime().ToString("yyyyMMdd-HH:mm:ss"));//transacttime
                        }
                        if (!args.ExecutionReport.isSetOrdType())
                        {
                            args.ExecutionReport.setString(40, info.Order.getOrdType().ToString());
                        }
                        if (!args.ExecutionReport.isSetPrice())
                        {
                            args.ExecutionReport.setString(44, info.Order.getPrice().ToString());
                        }
                        if (!args.ExecutionReport.isSetOrderQty())
                        {
                            args.ExecutionReport.setString(38, info.Order.getOrderQty().ToString());
                        }
                    }
                    this.Invoke(new ExecutionReportEventHandler(_quickFixWrapper_ExecutionReport), new object[] { sender, args });
                }
                catch (KeyNotFoundException e)
                {
                    Console.WriteLine(e.StackTrace);
                }
                catch (QuickFix.FieldNotFound e2)
                {
                    Console.WriteLine("Missing field: " + e2.field);
                }
            }
            else
            {
                try
                {
                    string msg = args.ExecutionReport.ToString();
                    //string origclordid = args.ExecutionReport.getClOrdID().getValue ();
                    //char exectype = args.ExecutionReport.getExecType().getValue();
                    //if (exectype == ExecType.PENDING_CANCEL || exectype == ExecType.PENDING_REPLACE  || exectype == ExecType.CANCELED  || exectype == ExecType.REPLACE)
                    //{
                    //    origclordid = args.ExecutionReport.getOrigClOrdID().getValue();
                    //}
                    //if(args .ExecutionReport .getExecType 
                    //label4.Text = "FIX����->" + msg;
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "FIX", msg);
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "ҵ��", "�ɽ��ر�:" + key);

                    //char exectype=args.ExecutionReport.getExecType().getValue ();
                    //if (exectype == ExecType.NEW || exectype == ExecType.REPLACE)
                    //{
                    //    lastclordid = args.ExecutionReport.getClOrdID();
                    //    lastsymbol = args.ExecutionReport.getSymbol();
                    //    lastside = args.ExecutionReport.getSide();
                    //    groupBox1.Text = "����ί�б��->" + lastclordid.getValue();
                    //}
                    if (ordersAll.ContainsKey(key))
                    {
                        info = ordersAll[key];
                        if (exec == ExecType.REPLACE)
                        {
                            ordersAll.Remove(key);
                            ordersAll.Add(info.ClOrdID, info);
                            //RemovePersistOrder(key);
                        }
                        this.UpdateListView(this.listView1, executionReport, info);
                    }
                    else
                    {
                        Console.WriteLine(executionReport.getClOrdID().getValue() + " order exeution report received");
                    }
                }
                catch (KeyNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        private void UpdateListView(ListView view, ExecutionReport rpt, OrderInfo ord)
        {
            try
            {
                string key = null;
                NewOrderSingle order = ord.Order;
                char exec = rpt.getExecType().getValue();
                if (exec == ExecType.PENDING_CANCEL || exec == ExecType.CANCELED || exec == ExecType.PENDING_REPLACE || exec == ExecType.REPLACE)
                {
                    if (rpt.isSetOrigClOrdID())//�Ӵ��̶�ȡ�����ָ��û�б���origclordid
                    {
                        key = rpt.getOrigClOrdID().getValue();
                    }
                    else
                    {
                        key = rpt.getClOrdID().getValue();
                    }
                }
                else
                {
                    key = rpt.getClOrdID().getValue();
                }
                OrderViewItem item = (OrderViewItem)view.Items[key];
                if (item == null)
                {
                    if (view.Name == "listView1")
                    {
                        OrderViewItem item1 = new OrderViewItem(order);
                        view.Items.Insert(0, item1);
                        item1.UpdateValues(order, rpt);
                    }     
                }
                else
                {
                    if (view.Name == "listView1")
                    {
                        item.UpdateValues(order, rpt);
                    }                 
                }
            }
            catch (IncorrectDataFormat e)
            {
                Console.WriteLine(e.TargetSite);
            }
        }
        void _quickFixWrapper_OnDisconnection(object sender, EventArgs e)
        {
            //throw new Exception("The method or operation is not implemented.");
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(_quickFixWrapper_OnDisconnection), new object[] { sender, e });
            }
            else
            {
                if (isconnected)
                {
                    isconnected = false;
                    SessionMenuItem.Text = "����";
                    this.Text = "FIX Initiator:Off";
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "ҵ��", "�Ѿ��Ͽ�");
                }
                else
                {
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "����", "���ܽ�������");
                }
            }
        }

        void _quickFixWrapper_OnConnection(object sender, EventArgs e)
        {
            //throw new Exception("The method or operation is not implemented.");
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(_quickFixWrapper_OnConnection), new object[] { sender, e });
            }
            else
            {
                if (!isconnected)
                {
                    isconnected = true;
                    SessionMenuItem.Text = "�Ͽ�";
                    this.Text = "FIX Initiator:On";
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "ҵ��", "�Ѿ���������");
                    //this.
                }
            }
        } 

        private void SessionMenuItem_Click(object sender, EventArgs e)
        {
            if (!isconnected)
            {
                string logintype = "";
                switch (comboBox1.SelectedItem.ToString())
                {
                    case "�����ʻ�":
                        logintype = "B";
                        break;
                    case "�ʽ��ʻ�":
                        logintype = "Z";
                        break;
                    case "�ͻ�����":
                        logintype = "C";
                        break;
                    case "�ſ���":
                        logintype = "K";
                        break;
                    case "������":
                        logintype = "X";
                        break;
                    case "�ɶ�����":
                        logintype = "N";
                        break;
                }
                GSFIXConnectionOption option = new GSFIXConnectionOption(logintype, textBox4.Text, textBox5.Text);
                _quickFixWrapper.Connect(option);
            }
            else
            {
                _quickFixWrapper.Disconnect();
            }
        }

        //private void CreateOrder(QuickFix.Side side)
        //{
        //    if (textBox1.Text != string.Empty && textBox2.Text != string.Empty && textBox3.Text != string.Empty)
        //    {
        //        ClOrdID clordid =new ClOrdID ( DateTime.Now.ToString("yyMMddHHmmss"));//Ψһ��Ͷ����ָ����
        //        QuickFix.HandlInst inst = new QuickFix.HandlInst('1'); //1	=	Automated execution order, private, no Broker intervention
        //        //2	=	Automated execution order, public, Broker intervention OK
        //        //3	=	Manual order, best execution

        //        QuickFix.Account account  =new Account ("0103137186"); //2009  11 25 add  �˺�

        //        QuickFix.Symbol symbol = new QuickFix.Symbol(textBox1.Text);
        //        QuickFix.TransactTime time = new QuickFix.TransactTime();
        //        QuickFix.OrdType ordtype = new QuickFix.OrdType('2');//2	=	Limit
        //        QuickFix42.NewOrderSingle message = new QuickFix42.NewOrderSingle(clordid, inst, symbol, side, time, ordtype);
        //        message.setString(44, textBox3.Text);
        //        message.setString(38,textBox2.Text);
        //        message.setString(207, "SSE");   //207   sh �Ϻ�
        //        message.setString(1, "0002077141");//1  Account �˺�
        //        _quickFixWrapper.Send(message);
        //    }
        //    else
        //    {
        //        MessageBox.Show("ָ���������");
        //    }            
        //}
        private void CreateOrder()
        {
            if (textBox1.Text != string.Empty && textBox2.Text != string.Empty && comboBox2.SelectedIndex != -1)
            {
                string id = GetNextID();
                ClOrdID clordid = new ClOrdID(id);//Ψһ��Ͷ����ָ����
                //1	=	Automated execution order, private, no Broker intervention
                //2	=	Automated execution order, public, Broker intervention OK
                //3	=	Manual order, best execution
                QuickFix.HandlInst inst = new QuickFix.HandlInst();
                if (comboBox6.SelectedIndex == -1)
                {
                    inst.setValue('1');
                }
                else
                {
                    switch (comboBox6.SelectedItem.ToString())
                    {
                        case "ֱͨ˽��":
                            inst.setValue('1');
                            break;
                        case "ֱͨ����":
                            inst.setValue('2');
                            break;
                        case "����̨":
                            inst.setValue('3');
                            break;
                    }
                }

                QuickFix.Side side = new QuickFix.Side();
                switch (comboBox2.SelectedItem.ToString())
                {
                    case "����":
                        side.setValue('1');
                        break;
                    case "����":
                        side.setValue('2');
                        break;
                    case "����":
                        side.setValue('5');
                        break;
                    case "�깺":
                        side.setValue('D');
                        break;
                    case "���":
                        side.setValue('E');
                        break;
                }
                //QuickFix.Account account = new Account("0103137186"); //2009  11 25 add  �˺�
                QuickFix.OrdType ordtype = new QuickFix.OrdType();
                if (comboBox3.SelectedIndex == -1)
                {
                    ordtype.setValue('1');
                }
                else
                {
                    switch (comboBox3.SelectedItem.ToString())
                    {
                        case "�м�":
                            ordtype.setValue('1');
                            break;
                        case "�޼�":
                            ordtype.setValue('2');
                            break;
                    }
                }

                QuickFix.Symbol symbol = new QuickFix.Symbol(textBox1.Text);
                QuickFix.TransactTime time = new QuickFix.TransactTime();
                QuickFix42.NewOrderSingle message = new QuickFix42.NewOrderSingle(clordid, inst, symbol, side, time, ordtype);
                if (ordtype.getValue () == QuickFix.OrdType.LIMIT)
                {
                    message.setString(44, textBox3.Text);//Limit Price
                }
                message.setString(38, textBox2.Text);//Quantity
                if (textBox1.Text.StartsWith("60"))
                {
                    message.setString(207, "SSE");   //207   sh �Ϻ�
                }
                else if (textBox1.Text.StartsWith("00"))
                {
                    message.setString(207, "SZSE");   //207   sz ����
                }
                //message.setString(1, "0002077141");//1  Account �˺�
                if (!ordersAll.ContainsKey(id))
                {
                    OrderInfo info = new OrderInfo(message);
                    ordersAll.Add(id, info);
                }
                _quickFixWrapper.Send(message);
            }
            else
            {
                MessageBox.Show("ָ���������");
            }
        }
        private void CancelOrder(string id)
        {
            if (id != null)
            {
                if (ordersAll.ContainsKey(id))
                {
                    OrigClOrdID orig = new OrigClOrdID(id);
                    OrderInfo info = (OrderInfo)ordersAll[id];
                    ClOrdID clordid = new ClOrdID(GetNextID());
                    Symbol symbol=info.Order .getSymbol ();
                    QuickFix.Side side = info.Order.getSide();
                    TransactTime time = new TransactTime();
                    QuickFix42.OrderCancelRequest cxl = new OrderCancelRequest(orig ,clordid ,symbol ,side,time);
                    _quickFixWrapper.Send(cxl);
                }
            }
        }
        private void CheckOrder(string id)
        {
            if (id != null)
            {
                if (ordersAll.ContainsKey(id))
                {
                    OrderInfo info = (OrderInfo)ordersAll[id];
                    ClOrdID clordid = info.Order.getClOrdID();
                    Symbol symbol = info.Order.getSymbol();
                    QuickFix.Side side = info.Order.getSide();

                    QuickFix42.OrderStatusRequest chk = new OrderStatusRequest(clordid ,symbol ,side );
                    _quickFixWrapper.Send(chk);
                }
            }
        }
        private void RejectExecution(string id, ExecutionReport executionReport)
        {
            if (!ordersAll.ContainsKey(id))
            {
                OrderID ordid = executionReport.getOrderID();
                ExecID execid = executionReport.getExecID();
                QuickFix.Side side = executionReport.getSide();
                Symbol symbol = executionReport.getSymbol();
                DKReason dkreason = new DKReason (DKReason.NO_MATCHING_ORDER);
                QuickFix42.DontKnowTrade dk = new DontKnowTrade(ordid, execid, dkreason, symbol, side);
                _quickFixWrapper.Send(dk);
            }
        }
        private string GetNextID()
        {
            string str = DateTime.Now.ToString("yyMMddHHmmss-") + fClOrdID;
            Interlocked.Increment(ref fClOrdID);

            return str;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //QuickFix.Side side = new QuickFix.Side('1');//1	=	Buy ; 2	=	Sell
            //CreateOrder(side);
            CreateOrder();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //QuickFix.Side side = new QuickFix.Side('2');//1	=	Buy ; 2	=	Sell
            //CreateOrder(side);     
            comboBox2.SelectedIndex = -1;
            textBox1.Text = string.Empty;
            textBox2.Text = string.Empty;
            comboBox3.SelectedIndex = -1;
            textBox3.Text = string.Empty;
            comboBox4.SelectedIndex = -1;
            comboBox5.SelectedIndex = -1;
            comboBox6.SelectedIndex = -1;
            textBox7.Text = string.Empty;
            comboBox8.SelectedIndex = -1;
            textBox6.Text = string.Empty;
        }

        //private void button3_Click(object sender, EventArgs e)
        //{
        //    if (lastclordid != null)
        //    {
        //        OrigClOrdID origclordid= new OrigClOrdID(lastclordid.getValue());//ԭָ����
        //        ClOrdID cxlid =new ClOrdID ( DateTime.Now.ToString("yyMMddHHmmss"));//�������
        //        QuickFix.TransactTime time = new QuickFix.TransactTime();
        //        QuickFix42.OrderCancelRequest cxl = new QuickFix42.OrderCancelRequest(origclordid, cxlid, lastsymbol, lastside, time);
        //        _quickFixWrapper.Send(cxl);
        //    }
        //}       
        internal class OrderViewItem : ListViewItem
        {
            // Fields
            private int stratidtag;
            private NewOrderSingle order;
            private char ordstatus;
            private const int FIELDORDTYPE = 40;
            // Methods
            internal OrderViewItem(NewOrderSingle order)
                : this(order, FIELDORDTYPE)
            {

                //this.UpdateValues();
            }
            internal OrderViewItem(NewOrderSingle order, int algofield)
                : base(new string[10])
            {
                this.order = order;
                this.Name = order.getClOrdID().getValue();
                stratidtag = algofield;
            }

            internal void UpdateValues(NewOrderSingle fixorder, ExecutionReport rpt)
            {
                try
                {
                    double avgpx = rpt.getAvgPx().getValue();
                    double cumqty = rpt.getCumQty().getValue();
                    double price = rpt.getPrice().getValue();
                    double qty = rpt.getOrderQty().getValue();
                    char status = rpt.getOrdStatus().getValue();
                    //string ordtype = rpt.getOrdType().ToString();
                    switch (status)
                    {
                        case QuickFix.OrdStatus.PARTIALLY_FILLED:
                            base.BackColor = Color.SkyBlue;
                            break;

                        case QuickFix.OrdStatus.FILLED:
                            base.BackColor = Color.Aquamarine;
                            break;

                        case QuickFix.OrdStatus.CANCELED:
                            base.BackColor = Color.Pink;
                            break;
                    }
                    string priceDisplay = "F2";
                    string pctDisplay = "P0";
                    string numDisplay = "N0";
                    double pct = cumqty / order.getOrderQty().getValue();
                    if (rpt.getExecType().getValue() == ExecType.REPLACE)
                    {
                        string clordid = fixorder.getClOrdID().getValue();
                        base.SubItems[0].Text = clordid;
                        base.SubItems[1].Text = rpt.getTransactTime().getValue().ToLocalTime().ToString("HH:mm:ss");
                        this.Name = clordid;
                        this.order = fixorder;
                    }
                    else
                    {
                        base.SubItems[0].Text = this.order.getClOrdID().getValue();
                        base.SubItems[1].Text = order.getTransactTime().getValue().ToLocalTime().ToString("HH:mm:ss");
                    }


                    base.SubItems[2].Text = this.order.getSymbol().getValue();
                    base.SubItems[3].Text = FixDataDictionary.getEnumString(FixDataDictionary.FixField.Side, this.order.getSide().ToString());
                    if (stratidtag != FIELDORDTYPE)
                    {
                        if (order.isSetField(stratidtag))
                        {
                            base.SubItems[4].Text = order.getField(stratidtag);
                        }
                    }
                    else
                    {
                        if (rpt.isSetOrdType())
                        {
                            base.SubItems[4].Text = FixDataDictionary.getEnumString(FixDataDictionary.FixField.OrdType, rpt.getOrdType().ToString());
                        }
                        else
                        {
                            base.SubItems[4].Text = FixDataDictionary.getEnumString(FixDataDictionary.FixField.OrdType, this.order.getOrdType().ToString());
                        }
                    }


                    if (rpt.isSetOrderQty())
                    {
                        base.SubItems[5].Text = qty.ToString(numDisplay);
                    }
                    else
                    {
                        base.SubItems[5].Text = this.order.getOrderQty().getValue().ToString();
                    }
                    if (rpt.isSetPrice())
                    {
                        base.SubItems[6].Text = price.ToString(priceDisplay);
                    }
                    else
                    {
                        base.SubItems[6].Text = this.order.getPrice().getValue().ToString(priceDisplay);
                    }
                    base.SubItems[7].Text = avgpx.ToString(priceDisplay);

                    base.SubItems[8].Text = pct.ToString(pctDisplay);

                    base.SubItems[9].Text = FixDataDictionary.getEnumString(FixDataDictionary.FixField.OrderStatus, status.ToString());
                    ordstatus = status;
                }
                catch (QuickFix.FieldNotFound e)
                {
                    Console.WriteLine("Missing field " + e.field);
                }

                //order. 
            }
            internal void UpdateValues(OrderCancelReject rjt)
            {
                char status = rjt.getOrdStatus().getValue();
                switch (status)
                {
                    case QuickFix.OrdStatus.PARTIALLY_FILLED:
                        base.BackColor = Color.SkyBlue;
                        break;

                    case QuickFix.OrdStatus.FILLED:
                        base.BackColor = Color.Aquamarine;
                        break;

                    case QuickFix.OrdStatus.CANCELED:
                        base.BackColor = Color.Pink;
                        break;
                }

                base.SubItems[9].Text = FixDataDictionary.getEnumString(FixDataDictionary.FixField.OrderStatus, status.ToString());
                ordstatus = status;
            }
            //public void Replace(NewOrderSingle replacement)
            //{
            //    order = replacement;
            //    this.Name = order.getClOrdID().getValue();
            //}

            // Properties
            internal NewOrderSingle Order
            {
                get
                {
                    return this.order;
                }
            }
            internal char OrdStatus
            {
                get
                {
                    return ordstatus;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            QuickFix42.Message message = new QuickFix42.Message(new MsgType("U003"));
            //message.setString(35, "U003");
            //message.setString(1, comboBox1.SelectedItem.ToString());
            //message .setString (
            _quickFixWrapper.Send(message);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            QuickFix42.Message message = new QuickFix42.Message(new MsgType("U002"));
            //message.setString(35, "U003");
            //message.setString(1, comboBox1.SelectedItem.ToString());
            //message .setString (
            _quickFixWrapper.Send(message);
        }
    }
    public class EnumValuePair
    {
        public string Enum;
        public string Value;
    }

    public abstract class FixField
    {
        protected string p_name;
        protected uint p_number;
        public string TagName
        {
            get
            {
                return p_name;
            }
        }
        public uint TagNumber
        {
            get
            {
                return p_number;
            }
        }
    }

    public class Side : FixField
    {
        public Side()
        {
            p_name = "Side";
            p_number = 54;
        }
    }
    public static class FixDataDictionary
    {
        public enum FixField { Side, OrdType, TimeInForce, IDSource, OrderStatus, ExecType, OrdRjtReason, CxlRjtReason, BusinessRejectReason };

        static Dictionary<FixField, Dictionary<string, string>> enumMap;

        static FixDataDictionary()
        {
            enumMap = new Dictionary<FixField, Dictionary<string, string>>();

            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("����", "1");
            dict.Add("����", "2");
            dict.Add("����", "3");
            enumMap.Add(FixField.Side, dict);

            Dictionary<string, string> ordTypeDict = new Dictionary<string, string>();
            ordTypeDict.Add("�м�", "1");
            ordTypeDict.Add("�޼�", "2");
            ordTypeDict.Add("Stop", "3");
            ordTypeDict.Add("Stop Limit", "4");
            ordTypeDict.Add("Market on Close", "5");
            enumMap.Add(FixField.OrdType, ordTypeDict);

            Dictionary<string, string> tifDict = new Dictionary<string, string>();
            tifDict.Add("Day", "0");
            tifDict.Add("GTC", "1");
            tifDict.Add("OPG", "2");
            tifDict.Add("IOC", "3");
            tifDict.Add("FOK", "4");
            tifDict.Add("GTX", "5");
            tifDict.Add("GTD", "6");
            tifDict.Add("At Close", "7");
            enumMap.Add(FixField.TimeInForce, tifDict);

            dict = new Dictionary<string, string>();
            dict.Add("�µ�", "0");
            dict.Add("����", "1");
            dict.Add("�ɽ�", "2");
            dict.Add("DFD", "3");
            dict.Add("����", "4");
            dict.Add("�޸�", "5");
            dict.Add("����", "6");
            dict.Add("��֤", "7");
            dict.Add("�ܾ�", "8");
            dict.Add("��ͣ", "9");
            dict.Add("�ӵ�", "A");
            dict.Add("����", "B");
            dict.Add("����", "C");
            dict.Add("����", "D");
            dict.Add("����", "E");
            //dict.Add("����", "F");
            //dict.Add("����", "G");
            //dict.Add("ȡ��", "H");
            //dict.Add("״̬", "I");
            enumMap.Add(FixField.OrderStatus, dict);

            dict = new Dictionary<string, string>();
            dict.Add("ȯ�̾���", "0");
            dict.Add("δ֪���״���", "1");
            dict.Add("������������", "2");
            dict.Add("ί�м۸񳬳�ͣ���", "3");
            dict.Add("ί��ʱ�䲻��", "4");
            dict.Add("δָ֪��", "5");
            dict.Add("�ظ�ָ��", "6");
            dict.Add("���ͷָ���ظ�", "7");
            dict.Add("�Ƿ�ָ�����", "8");
            enumMap.Add(FixField.OrdRjtReason, dict);

            dict = new Dictionary<string, string>();
            dict.Add("�µ�", "0");
            dict.Add("����", "1");
            dict.Add("�ɽ�", "2");
            dict.Add("DFD", "3");
            dict.Add("����", "4");
            dict.Add("�޸�", "5");
            dict.Add("����", "6");
            dict.Add("��֤", "7");
            dict.Add("�ܾ�", "8");
            dict.Add("��ͣ", "9");
            dict.Add("����", "A");
            dict.Add("����", "B");
            dict.Add("����", "C");
            dict.Add("����", "D");
            dict.Add("����", "E");
            dict.Add("����", "F");
            dict.Add("����", "G");
            dict.Add("ȡ��", "H");
            dict.Add("״̬", "I");
            enumMap.Add(FixField.ExecType, dict);

            dict = new Dictionary<string, string>();
            dict.Add("����������", "0");
            dict.Add("δָ֪��", "1");
            dict.Add("ȯ�̾���", "2");
            dict.Add("����״̬��", "3");
            enumMap.Add(FixField.CxlRjtReason, dict);

            dict = new Dictionary<string, string>();
            dict.Add("����ԭ��", "0");
            dict.Add("δ֪ID", "1");
            dict.Add("δ֪֤ȯ", "2");
            dict.Add("��֧�ֵ���Ϣ����", "3");
            dict.Add("Ӧ�ó��򲻿ɵ�", "4");
            dict.Add("ȱ��������ֶ���Ϣ", "5");
            enumMap.Add(FixField.BusinessRejectReason, dict);
        }

        public static EnumValuePair[] getEnumValues(FixField f)
        {
            EnumValuePair[] enums = null;
            try
            {

                Dictionary<string, string> eDict = enumMap[f];

                if (eDict.Count > 0)
                {
                    enums = new EnumValuePair[eDict.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, string> kvp in eDict)
                    {
                        enums[i] = new EnumValuePair();
                        enums[i].Enum = kvp.Key;
                        enums[i].Value = kvp.Value;
                        ++i;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("getEnumValues() caught exception: " + e.Message);
            }
            return enums;
        }

        public static EnumValuePair getEnumValue(FixField f, string enumString)
        {
            EnumValuePair evp = null;
            try
            {
                Dictionary<string, string> eDict = enumMap[f];
                evp = new EnumValuePair();
                evp.Enum = enumString;
                evp.Value = eDict[enumString];
            }
            catch (Exception e)
            {
                Console.WriteLine("getEnumValue() caught exception: " + e.Message);
                evp = null;
            }
            return evp;
        }
        public static string getEnumString(FixField f, string enumValue)
        {
            string enumstr = null;
            Dictionary<string, string> eDict = enumMap[f];
            foreach (KeyValuePair<string, string> evp in eDict)
            {
                if (evp.Value == enumValue)
                {
                    enumstr = evp.Key;
                    break;
                }
            }
            return enumstr;
        }
    }
    public class OrderInfo
    {
        private NewOrderSingle order;
        //private ArrayList rptlist;
        private SortedList ordList;
        private string rootclordid;
        private string ordid;
        public string RootClOrdID
        {
            get
            {
                return rootclordid;
            }
        }
        public string ClOrdID
        {
            get
            {
                return order.getClOrdID().getValue();
            }
        }
        public string OrderID
        {
            get
            {
                return ordid;
            }
            //set
            //{
            //    ordid = value;
            //}
        }
        //public ArrayList Executions
        //{
        //    get
        //    {
        //        return rptlist;
        //    }
        //}
        public NewOrderSingle Order
        {
            get
            {
                return order;
            }
        }
        public OrderInfo(NewOrderSingle fixorder)
        {
            rootclordid = fixorder.getClOrdID().getValue();
            order = fixorder;
            ordList = new SortedList();
            //rptlist = new ArrayList();
            ordid = null;
            ordList.Add(order.getClOrdID().getValue(), order);
        }
        public void Add(ExecutionReport report)
        {
            char exec = report.getExecType().getValue();
            if (exec == ExecType.REPLACE)
            {
                ordid = report.getOrderID().getValue();
                string clordid = report.getClOrdID().getValue();
                UpdateOrder(clordid);
            }
            else if (exec == ExecType.NEW)
            {
                if (report.isSetOrderID())
                {
                    ordid = report.getOrderID().getValue();
                }
            }
            else if (ordid == null)
            {
                if (report.isSetOrderID())
                {
                    ordid = report.getOrderID().getValue();
                }
            }
            //ExecutionReportViewItem item = new ExecutionReportViewItem(report);
            //rptlist.Add(item);
        }
        public void Add(OrderCancelReject reject)
        {
            //ExecutionReportViewItem item = new ExecutionReportViewItem(reject);
            //rptlist.Add(item);
        }
        public void Add(OrderCancelRequest request)
        {
            //cxlrplList.Add(request);
            ordList.Add(request.getClOrdID().getValue(), request);
        }
        public void Add(OrderCancelReplaceRequest request)
        {
            //cxlrplList.Add(request);
            //order.
            ordList.Add(request.getClOrdID().getValue(), request);
        }
        private void UpdateOrder(string newclordid)
        {
            if (ordList.ContainsKey(newclordid))
            {
                QuickFix.Message rpl = (QuickFix.Message)ordList[newclordid];
                NewOrderSingle order1 = new NewOrderSingle();
                foreach (StringField sf in rpl)
                {
                    order1.setString(sf.getField(), sf.getValue());
                }
                if (order1.isSetField(41))
                {
                    order1.removeField(41);//origcloridid
                }
                order = order1;
            }
        }
    }
}