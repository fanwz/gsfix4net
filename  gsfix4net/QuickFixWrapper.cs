using System;
using System.Collections.Generic;
using System.Text;
using QuickFix;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace QuickFixInitiator
{
    class QuickFixWrapper : QuickFix.MessageCracker, QuickFix.Application
    {
        private string settingfile = "initiator.cfg";
        private QuickFix.SessionID _ssnid;
        private QuickFix.SocketInitiator _socketInitiator;//��ʹ��ThreadedSocketInitiator
        private QuickFix.FileStoreFactory _messageStoreFactory;//��ʹ���ļ����ڴ桢���ݿ�
        private QuickFix.SessionSettings _settings;
        private QuickFix.FileLogFactory _logFactory;//��ʹ����Ļ�����ݿ⡢�ļ�
        private QuickFix42.MessageFactory _messageFactory;
        private Hashtable fCancelRequests;
        private GSFIXConnectionOption option;
        public event EventHandler OnError;
        public event EventHandler OnConnection;
        public event EventHandler OnDisconnection;
        public event OrderCancelRejectEventHandler OrderCancelReject;
        public event ExecutionReportEventHandler ExecutionReport;
        public QuickFixWrapper()
        {
            Init();
            this.fCancelRequests = new Hashtable();
        }
    
        public void onCreate(QuickFix.SessionID pSessionID) 
        {
            _ssnid = pSessionID;
        }
        public void onLogon(QuickFix.SessionID pSessionID) 
        {
            if (OnConnection != null)
            {
                OnConnection(this, EventArgs.Empty);
            }
        }
        public void onLogout(QuickFix.SessionID pSessionID) 
        {
            if (OnDisconnection != null)
            {
                OnDisconnection(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// ���͸��Զ˵ĻỰ�㱨�ģ�ͨ��CONSOLE�����
        /// </summary>
        /// <param name="message"></param>
        /// <param name="pSessionID"></param>
        public void toAdmin(QuickFix.Message message, QuickFix.SessionID pSessionID)
        {
            if (message is QuickFix42 .Logon )//ȷ���ǵ�½����
            {
                string rawdata = option .Rawdata ;
                message.setString(96, rawdata);
            }
            else if (message is QuickFix42.Logout)
            {
                if (message.isSetField(58))
                {
                    string note = message.getString(58);
                    if (OnError != null)
                    {
                        OnError(note, EventArgs.Empty);
                    }
                }
            }
            //Console.WriteLine("Enter toAdmin");
            //Console.WriteLine(message.ToString());
            //Console.WriteLine("Exit toAdmin");
          //  MessageBox.Show("toAdmin"+message .ToString ());  //20091229 
        }
        /// <summary>
        /// ���͸��Զ˵�Ӧ�ò㱨�ģ���Ҫҵ���߼�����
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pSessionID"></param>
        public void toApp(QuickFix.Message pMessage, QuickFix.SessionID pSessionID) 
        { 
            //����������ҵ���߼�������ת����LOG��
            //MessageBox.Show("toApp "+pMessage .ToString ());
        } 
        /// <summary>
        /// �Զ˷����ĻỰ�㱨�ģ�ͨ��CONSOLE�����
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pSessionID"></param>
        public void fromAdmin(QuickFix.Message pMessage, QuickFix.SessionID pSessionID) 
        {
            //Console.WriteLine("Enter fromAdmin");
            //Console.WriteLine(pMessage.ToString());
            //MessageBox.Show("fromAdmin ����"+pMessage .ToString ());  //20091229
            //Console.WriteLine("Exit fromAdmin");
        }    
        /// <summary>
        /// ���ԶԶ˵�Ӧ�ò㱨�ģ���Ҫҵ���߼�����
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pSessionID"></param>
        public void fromApp(QuickFix.Message pMessage, QuickFix.SessionID pSessionID) 
        {
            string msgtype = pMessage.getHeader ().getString  (35);
            
            if (msgtype.StartsWith("Ans"))
            {
                //Console.WriteLine(pMessage.ToString());
                 switch (msgtype)
                {
                     case "Ans003":
                         //string ccy=pMessage .getField (
                     case "Ans002":
                         if (OnError != null)
                         {
                             OnError(pMessage.ToString(), EventArgs.Empty);
                         }
                        break;
                }
            }
            else
            {
                base.crack(pMessage, pSessionID);//����Ĭ�ϴ���������
            }
            pMessage.Dispose();//�����ֳ�����Ҫ
        }

        public void Connect(GSFIXConnectionOption o)
        {
            if (o.Rawdata == "")
            {
                if (OnError != null)
                {
                    OnError("δ��ȷ���õ�½��֤��Ϣ", EventArgs.Empty);
                }
            }
            else
            {
                option = o;
                this._socketInitiator.start();
                Session session = Session.lookupSession(_ssnid);
                if ((session != null) && !session.isLoggedOn())
                {
                    session.logon();
                }
            }
        }
        public void Disconnect()
        {
            Session session = Session.lookupSession(_ssnid);
            if ((session != null) && session.isLoggedOn())
            {
                session.logout();
            }
            //this._socketInitiator.stop();//����ע��������䣬�����޷������ֳ�
        }
        public int IncomingSeq
        {
            get
            {
                Session session = Session.lookupSession(_ssnid);
                return session.getExpectedTargetNum();
            }
            set
            {
                Session session = Session.lookupSession(_ssnid);
                session.setNextTargetMsgSeqNum (value);
            }
        }
        public int OutgoingSeq
        {
            get
            {
                Session session = Session.lookupSession(_ssnid);
                return session.getExpectedSenderNum();
            }
            set
            {
                Session session = Session.lookupSession(_ssnid);
                session.setNextSenderMsgSeqNum(value);
            }
        }
        public void Send(QuickFix42.NewOrderSingle message)
        {
            Session.sendToTarget(message, _ssnid);
        }
        public void Send(QuickFix42.OrderCancelRequest message)
        {
            string ClOrdID = message.getClOrdID().getValue();
            if (!fCancelRequests.ContainsKey(ClOrdID))
            {
                this.fCancelRequests.Add(ClOrdID, message);
                Session.sendToTarget(message, _ssnid);
            }
            else
            {
                Console.WriteLine("Order " + ClOrdID + " already pending cancel process");
            }
        }
        public void Send(QuickFix42.OrderStatusRequest message)
        {
            Session.sendToTarget(message, _ssnid);
        }
        public void Send(QuickFix42.DontKnowTrade message)
        {
            Session.sendToTarget(message, _ssnid);
        }
        public void Send(QuickFix42.Message message)
        {
            Session.sendToTarget(message, _ssnid);
        }
        public override void onMessage(QuickFix42.ExecutionReport report, SessionID sessionID)
        {
            if (ExecutionReport != null)
            {
                ExecutionReport(sessionID, new ExecutionReportEventArgs(report));
            }  
        }
        public override void onMessage(QuickFix42.OrderCancelReject message, SessionID SessionID)
        {
            if (OrderCancelReject != null)
            {
                OrderCancelReject(SessionID, new OrderCancelRejectEventArgs(message));
            }    
        }

        private void Init()
        {
            try
            {
                this._settings = new QuickFix.SessionSettings(settingfile);
                this._messageStoreFactory = new QuickFix.FileStoreFactory(this._settings);
                this._logFactory = new QuickFix.FileLogFactory(this._settings);
                this._messageFactory = new QuickFix42.MessageFactory();
                this._socketInitiator = new QuickFix.SocketInitiator  (this, _messageStoreFactory, this._settings, this._logFactory, _messageFactory);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
    }

    //�ڵ�¼��Ϣ��RawData�������̨��¼���������ֶ�,��ʽΪ:
    //��¼����:��¼��ʶ:��������:
    // n  ��¼����:
    //��B����ʾ�������ʻ���¼  -��¼��ʶΪ�����ʻ�
    //��Z����ʾ���ʽ��ʻ���¼  -��¼��ʶΪ�ʽ��ʻ�
    //��C����ʾ�Կͻ������¼  -��¼��ʶΪ�ͻ����루�������˴��룩
    //��K����ʾ�Դſ���¼      -��¼��ʶΪ�ſ��� 
    //��X����ʾ�����˵�¼      -��¼��ʶΪ������
    //��N����ʾ�ùɶ������¼  -��¼��ʶΪ�ɶ�����
    //������ʾ�Թɶ������¼  -��¼��ʶΪ��Ӧ�г��Ĺɶ����루����δʵ�ָù��ܣ� 
    //
    //��ϸ��Ϣ�ɲο�������֤ȯ��Χ���׽ӿ�˵��V3.05���û���¼(420301)�ӿ�
    public class GSFIXConnectionOption
    {
        private string rawdata;
        public GSFIXConnectionOption(string logintype,string id, string pwd)
        {
            switch (logintype)
            {
                case "B":
                case "Z":
                case "C":
                case "K":
                case "X":
                case "N":
                    rawdata = logintype + ":" + id + ":" + EncodePassword(id,pwd );
                    break;
                default :
                    rawdata = "";
                    break;
            }
        }
        public string Rawdata
        {
            get
            {
                return rawdata;
            }
        }
        private string EncodePassword(string key, string password)
        {
            int enclvl = 6;
            StringBuilder enc = new StringBuilder();
            int nSrcDataLen = password.Length;
            int nDestDataBufLen = 2 * nSrcDataLen;
            nDestDataBufLen = nDestDataBufLen < 32 ? 32 : nDestDataBufLen;
            KDEncode(enclvl, password, nSrcDataLen, enc, nDestDataBufLen, key, key.Length);
            return enc.ToString();
        }
        [DllImport("KDEncodeCli.dll")]
        public static extern int KDEncode(int nEncode_Level, string pSrcData, int nSrcDataLen, StringBuilder pDestData, int nDestDataBufLen, string pKey, int nKeyLen);
       
    }
    public class ExecutionReportEventArgs : EventArgs
    {
        // Fields
        private QuickFix42.ExecutionReport report;

        // Methods
        public ExecutionReportEventArgs(QuickFix42.ExecutionReport report)
        {
            this.report = report;
        }

        // Properties
        public QuickFix42.ExecutionReport ExecutionReport
        {
            get
            {
                return this.report;
            }
        }
    }
    public delegate void ExecutionReportEventHandler(object sender, ExecutionReportEventArgs args);
    public class OrderCancelRejectEventArgs : EventArgs
    {
        // Fields
        private QuickFix42.OrderCancelReject reject;

        // Methods
        public OrderCancelRejectEventArgs(QuickFix42.OrderCancelReject reject)
        {
            this.reject = reject;
        }

        // Properties
        public QuickFix42.OrderCancelReject OrderCancelReject
        {
            get
            {
                return this.reject;
            }
        }
    }
    public delegate void OrderCancelRejectEventHandler(object sender, OrderCancelRejectEventArgs args);
    public class FundStatusReportEventArgs : EventArgs
    {
        private string ccy;
        private double bal;
        private double avbl;
        public string Currency
        {
            get
            {
                return ccy;
            }
        }
        public double Balance
        {
            get
            {
                return bal;
            }
        }
        public double Available
        {
            get
            {
                return avbl;
            }
        }
        public FundStatusReportEventArgs(string currency, string balance, string available)
        {
            ccy = currency;
            double.TryParse(balance, out bal);
            double.TryParse(available, out avbl);
        }
    }
    public delegate void FundStatusReportEventHandler(object sender,FundStatusReportEventArgs args);
    public class PostionStatusReportEventArgs : EventArgs
    {
        public PostionStatusReportEventArgs()
        {
        }
    }
}
     
