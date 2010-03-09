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
            //listBox1.Items.Add("银行帐户");
            //listBox1.Items.Add("资金帐户");
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
        /// 所有的撤单拒绝报文均在GUI回显
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
                label4.Text = "FIX报文->" + msg;
                dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), msg);
            }
           dataGridView1 .Rows .Insert  (0,DateTime .Now .ToString (),"撤单");
        }
        /// <summary>
        /// 所有的成交回报（回报种类参考FIX协议）报文均在GUI回显。其中new和replace回报将登记部分指令信息，可以用于撤单
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
                label4.Text = "FIX报文->" + msg;
                dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), msg);

                char exectype=args.ExecutionReport.getExecType().getValue ();
                if (exectype == ExecType.NEW || exectype == ExecType.REPLACE)
                {
                    lastclordid = args.ExecutionReport.getClOrdID();
                    lastsymbol = args.ExecutionReport.getSymbol();
                    lastside = args.ExecutionReport.getSide();
                    groupBox1.Text = "最新委托编号->"+lastclordid .getValue ();
                }
            }
            dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "成交回报。。");
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
                    SessionMenuItem.Text = "联线";
                    this.Text = "FIX Initiator:Off";
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "已经断开");
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
                    SessionMenuItem.Text = "断开";
                    this.Text = "FIX Initiator:On";
                    dataGridView1.Rows.Insert(0, DateTime.Now.ToString(), "已经连接上了");
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
                    case "银行帐户":
                        logintype = "B";
                        break;
                    case "资金帐户":
                        logintype = "Z";
                        break;
                    case "客户代码":
                        logintype = "C";
                        break;
                    case "磁卡号":
                        logintype = "K";
                        break;
                    case "代理人":
                        logintype = "X";
                        break;
                    case "股东内码":
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
                ClOrdID clordid =new ClOrdID ( DateTime.Now.ToString("yyMMddHHmmss"));//唯一的投资者指令编号
                QuickFix.HandlInst inst = new QuickFix.HandlInst('1'); //1	=	Automated execution order, private, no Broker intervention
                //2	=	Automated execution order, public, Broker intervention OK
                //3	=	Manual order, best execution

                QuickFix.Account account  =new Account ("0103137186"); //2009  11 25 add  账号

                QuickFix.Symbol symbol = new QuickFix.Symbol(textBox1.Text);
                QuickFix.TransactTime time = new QuickFix.TransactTime();
                QuickFix.OrdType ordtype = new QuickFix.OrdType('2');//2	=	Limit
                QuickFix42.NewOrderSingle message = new QuickFix42.NewOrderSingle(clordid, inst, symbol, side, time, ordtype);
                message.setString(44, textBox3.Text);
                message.setString(38,textBox2.Text);
                message.setString(207, "SSE");   //207   sh 上海
                message.setString(1, "0002077141");//1  Account 账号
                _quickFixWrapper.Send(message);
            }
            else
            {
                MessageBox.Show("指令参数不足");
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
                OrigClOrdID origclordid= new OrigClOrdID(lastclordid.getValue());//原指令编号
                ClOrdID cxlid =new ClOrdID ( DateTime.Now.ToString("yyMMddHHmmss"));//撤单编号
                QuickFix.TransactTime time = new QuickFix.TransactTime();
                QuickFix42.OrderCancelRequest cxl = new QuickFix42.OrderCancelRequest(origclordid, cxlid, lastsymbol, lastside, time);
                _quickFixWrapper.Send(cxl);
            }
        }       
    }
}