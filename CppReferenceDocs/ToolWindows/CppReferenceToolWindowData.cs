// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Windows.Controls;
using Microsoft.VisualStudio.Extensibility.UI;

namespace CppReferenceDocs.ToolWindows
{
    /// <summary>
    /// ViewModel for the CppReferenceToolWindowContent remote user control.
    /// </summary>
    [DataContract]
    internal class CppReferenceToolWindowData : NotifyPropertyChangedObject
    {
        public CppReferenceToolWindowData()
        {
            OnLayoutUpdated = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            {
                AddressTextBoxWidth = 123;
                return Task.CompletedTask;
            });
            OnGoButtonPressed = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            {
                _browser.Navigate(URL);
                return Task.CompletedTask;
            });
            OnSearchButtonPressed = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            {
                _browser.Navigate(URL);
                return Task.CompletedTask;
            });
            //HelloCommand = new AsyncCommand((parameter, clientContext, cancellationToken) =>
            //{
            //    Text = $"Hello {parameter as string}!";
            //    return Task.CompletedTask;
            //});
        }

        private string _name = string.Empty;

        [DataMember]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _text = string.Empty;

        [DataMember]
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        private WebBrowser _browser = new();

        [DataMember]
        public WebBrowser DocsBrowser
        {
            get => _browser;
            set => SetProperty(ref _browser, value);
        }

        private readonly TextBox _addressTextBox = new();

        [DataMember]
        public double AddressTextBoxWidth
        {
            get => _addressTextBox.Width;
            set { _addressTextBox.Width = value; }
        }

        private Uri _uri = new("");

        [DataMember]
        public Uri URL
        {
            get => _uri;
            set => SetProperty(ref _uri, value);
        }


        [DataMember] public AsyncCommand OnGoButtonPressed { get; }
        [DataMember] public AsyncCommand OnSearchButtonPressed { get; }
        [DataMember] public AsyncCommand OnLayoutUpdated { get; }
    }
}
