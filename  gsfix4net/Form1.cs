using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QuickFix;

namespace QuickFixInitiator
{
    public partial class Form1 : Form
    {
        QuickFixWrapper _quickFixWrapper = new QuickFixWrapper();
        bool isconnected = false;
        ClOrdID  lastclordid=null  ;
        Symbol lastsymbol = null;
        Side lastside = null;
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
            string msg = (string)sender;
            MessageBox.Show(msg);
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
                this.Invoke(new OrderCancelRejectEventHandler(_quickFixWrapper_OrderCancelReject), new object[] { sender, args  });
            }
            else
            {
                string msg = args.OrderCancelReject.ToString();
                label4.Text = "FIX����->" + msg;
                dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), msg);
            }
           dataGridView1 .Rows .Insert  (0,DateTime .Now .ToString (),"����");
        }
        /// <summary>
        /// ���еĳɽ��ر����ر�����ο�FIXЭ�飩���ľ���GUI���ԡ�����new��replace�ر����Ǽǲ���ָ����Ϣ���������ڳ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _quickFixWrapper_ExecutionReport(object sender, ExecutionReportEventArgs args)
        {
            //throw new Exception("The method or operation is not implemented.");
            if (this.InvokeRequired)
            {
                this.Invoke(new ExecutionReportEventHandler(_quickFixWrapper_ExecutionReport), new object[] { sender, args  });
            }
            else
            {
                string msg = args.ExecutionReport.ToString();                
                label4.Text = "FIX����->" + msg;
                dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), msg);

                char exectype=args.ExecutionReport.getExecType().getValue ();
                if (exectype == ExecType.NEW || exectype == ExecType.REPLACE)
                {
                    lastclordid = args.ExecutionReport.getClOrdID();
                    lastsymbol = args.ExecutionReport.getSymbol();
                    lastside = args.ExecutionReport.getSide();
                    groupBox1.Text = "����ί�б��->"+lastclordid .getValue ();
                }
            }
            dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "�ɽ��ر�����");
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
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "�Ѿ��Ͽ�");
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
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "�Ѿ���������");
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

        private void CreateOrder(QuickFix.Side side)
        {
            if (textBox1.Text != string.Empty && textBox2.Text != string.Empty && textBox3.Text != string.Empty)
            {
                ClOrdID clordid =new ClOrdID ( DateTime.Now.ToString("yyMMddHHmmss"));//Ψһ��Ͷ����ָ����
                QuickFix.HandlInst inst = new QuickFix.HandlInst('1'); //1	=	Automated execution order, private, no Broker intervention
                //2	=	Automated execution order, public, Broker intervention OK
                //3	=	Manual order, best execution

                QuickFix.Account account  =new Account ("0103137186"); //2009  11 25 add  �˺�

                QuickFix.Symbol symbol = new QuickFix.Symbol(textBox1.Text);
                QuickFix.TransactTime time = new QuickFix.TransactTime();
                QuickFix.OrdType ordtype = new QuickFix.OrdType('2');//2	=	Limit
                QuickFix42.NewOrderSingle message = new QuickFix42.NewOrderSingle(clordid, inst, symbol, side, time, ordtype);
                message.setString(44, textBox3.Text);
                message.setString(38,textBox2.Text);
                message.setString(207, "SSE");   //207   sh �Ϻ�
                message.setString(1, "0002077141");//1  Account �˺�
                _quickFixWrapper.Send(message);
            }
            else
            {
                MessageBox.Show("ָ���������");
            }            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            QuickFix.Side side = new QuickFix.Side('1');//1	=	Buy ; 2	=	Sell
            CreateOrder(side);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            QuickFix.Side side = new QuickFix.Side('2');//1	=	Buy ; 2	=	Sell
            CreateOrder(side);     
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (lastclordid != null)
            {
                OrigClOrdID origclordid= new OrigClOrdID(lastclordid.getValue());//ԭָ����
                ClOrdID cxlid =new ClOrdID ( DateTime.Now.ToString("yyMMddHHmmss"));//�������
                QuickFix.TransactTime time = new QuickFix.TransactTime();
                QuickFix42.OrderCancelRequest cxl = new QuickFix42.OrderCancelRequest(origclordid, cxlid, lastsymbol, lastside, time);
                _quickFixWrapper.Send(cxl);
            }
        }       
    }
}