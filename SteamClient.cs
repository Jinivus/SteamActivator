using Steam4NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace SteamActivator
{
    public class SteamClient
    {
        private int _user, _pipe;
        private bool _waitingForActivationResp = false;

        private string _result = "";

        private ISteam006 _steam006;
        private IClientEngine _clientEngine;
        private IClientBilling _clientBilling;
        private ISteamClient012 _steamClient012;

        private Thread callbackHandlerThread;
        public string ActivateKey(string key)
        {

            if (Utils.ValidateCDKey(key))
            {
                if (connectToSteam())
                {
                    callbackHandlerThread = new Thread(() =>
                    {
                        _waitingForActivationResp = true;
                        CallbackMsg_t callbackMsg = new CallbackMsg_t();
                        while (callbackHandlerThread.ThreadState != ThreadState.AbortRequested && callbackHandlerThread.ThreadState != ThreadState.Aborted)
                        {
                            while (Steamworks.GetCallback(_pipe, ref callbackMsg) && callbackHandlerThread.ThreadState != ThreadState.AbortRequested && callbackHandlerThread.ThreadState != ThreadState.Aborted)
                            {
                                switch (callbackMsg.m_iCallback)
                                {
                                    case PurchaseResponse_t.k_iCallback:
                                        onPurchaseResponse((PurchaseResponse_t)Marshal.PtrToStructure(callbackMsg.m_pubParam, typeof(PurchaseResponse_t)));
                                        break;
                                }

                                Steamworks.FreeLastCallback(_pipe);
                            }

                            Thread.Sleep(100);
                        }
                    }
                    );
                    callbackHandlerThread.Start();

                    _clientBilling.PurchaseWithActivationCode(key);

                    while (_waitingForActivationResp)
                        Thread.Sleep(100);

                    return _result;
                    
                }
            }
            return "Something went wrong";
        }



        private void onPurchaseResponse(PurchaseResponse_t purchaseResponse_t)
        {
            int result = purchaseResponse_t.m_EPurchaseResultDetail;
            switch (result)
            {
                /*53 equals too many activation attempts*/
                case 53:
                    callbackHandlerThread.Abort();
                    break;
            }
            _result = Utils.GetFriendlyEPurchaseResultDetailMsg(result);
            callbackHandlerThread.Abort();
            _waitingForActivationResp = false;
        }

        private bool connectToSteam()
        {
            var steamError = new TSteamError();

            if (!Steamworks.Load(true))
            {
                throw new SteamException("Steamworks failed to load.");
                return false;
            }

            _steam006 = Steamworks.CreateSteamInterface<ISteam006>();
            if (_steam006.Startup(0, ref steamError) == 0)
            {
                throw new SteamException("Steam startup failed.");
                return false;
            }

            _steamClient012 = Steamworks.CreateInterface<ISteamClient012>();
            _clientEngine = Steamworks.CreateInterface<IClientEngine>();

            _pipe = _steamClient012.CreateSteamPipe();
            if (_pipe == 0)
            {
                throw new SteamException("Failed to create a pipe.");
                return false;
            }

            _user = _steamClient012.ConnectToGlobalUser(_pipe);
            if (_user == 0 || _user == -1)
            {
                throw new SteamException("Failed to connect to global user.");
                return false;
            }

            _clientBilling = _clientEngine.GetIClientBilling<IClientBilling>(_user, _pipe);
            return true;
        }

        public class SteamException : Exception
        {
            private void DemonstrateException()
            {
                var ex1 = new Exception();
                var ex2 = new Exception("Test string");
                var ex3 = new Exception("Test string and InnerException", new Exception());

            }

            public SteamException(string message) : base("Something wrong")
            {

            }
        }
    }
}
