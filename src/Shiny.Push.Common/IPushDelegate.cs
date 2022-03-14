﻿using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushDelegate
    {
        /// <summary>
        /// This is called when the user taps/responds to a push notification
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task OnEntry(PushData data);


        /// <summary>
        /// Called when a push is received. BACKGROUND NOTE: if your app is in the background, you need to pass data parameters (iOS: content-available:1) to get this to fire
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task OnReceived(PushData data);


        /// <summary>
        /// This is called ONLY when the token changes, not during RequestAccess
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task OnTokenRefreshed(string token);
    }
}
