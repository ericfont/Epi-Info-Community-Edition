﻿using System;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Epi;
using Epi.Data;
using Epi.Fields;
using EpiDashboard.Rules;
using EpiDashboard.Controls;

namespace EpiDashboard
{
    /// <summary>
    /// Interaction logic for RatesControl.xaml
    /// </summary>
    /// /// <remarks>
    /// This gadget is used to generate a line listing for the fields in the current data source, to include
    /// any related data columns. Columns are not sorted by default but, if using an Epi Info 7 project, they
    /// may also be sorted by their tab order (also known as the order of entry).
    /// </remarks>
    /// 

    public partial class RatesControl : GadgetBase
    {
        #region Private Members
        private List<string> columnOrder = new List<string>();

        /// <summary>
        /// Bool used to determine if the gadget is being called directly from the Enter module (e.g. 'Interactive Rates' option)
        /// </summary>
        private bool isHostedByEnter;

        /// <summary>
        /// Used for maintaining the width when collapsing output
        /// </summary>
        private double currentWidth;

        private RequestUpdateStatusDelegate requestUpdateStatus;
        private CheckForCancellationDelegate checkForCancellation;


        #endregion // Private Members

        #region Events
        public event Mapping.RecordSelectedHandler RecordSelected;
        #endregion // Events

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public RatesControl()
        {
            InitializeComponent();
            Construct();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dashboardHelper">The dashboard helper object to attach</param>
        public RatesControl(DashboardHelper dashboardHelper)
        {
            InitializeComponent();
            this.DashboardHelper = dashboardHelper;
            this.IsHostedByEnter = false;
            Construct();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dashboardHelper">The dashboard helper object to attach</param>
        /// <param name="hostedByEnter">Whether the control is hosted by Enter</param>
        public RatesControl(DashboardHelper dashboardHelper, bool hostedByEnter)
        {
            InitializeComponent();
            this.DashboardHelper = dashboardHelper;
            this.IsHostedByEnter = hostedByEnter;
            Construct();
        }
        #endregion // Constructors

        #region Public Methods
        /// <summary>
        /// Returns the gadget's description as a string.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return "Rates Gadget";
        }

        /// <summary>
        /// Auto-selects a given page number from the list of pages
        /// </summary>
        /// <param name="pageNumber">The page number</param>
        public void SelectPageNumber(int pageNumber)
        {
            Parameters.ColumnNames.Clear();
            Parameters.ColumnNames.Add("Page " + pageNumber);
            if (IsHostedByEnter)
            {
                if (Parameters.ColumnNames.Contains("UniqueKey"))
                {
                    Parameters.ColumnNames.Remove("UniqueKey");
                }
                Parameters.ColumnNames.Add("UniqueKey");
            }
        }

        private DataGrid GetDataGrid()
        {
            DataGrid dg = null;
            foreach (UIElement element in panelMain.Children)
            {
                if (element is DataGrid)
                {
                    dg = element as DataGrid;
                }
            }

            return dg;
        }

        /// <summary>
        /// Copies a grid's output to the clipboard
        /// </summary>
        protected override void CopyToClipboard()
        {
            DataGrid dg = GetDataGrid();

            if (dg != null)
            {
                if (dg.ItemsSource is DataView)
                {
                    Common.CopyDataViewToClipboard(dg.ItemsSource as DataView);
                }
                else if (dg.ItemsSource is ListCollectionView)
                {
                    ListCollectionView lcv = dg.ItemsSource as ListCollectionView;
                    if (lcv.SourceCollection is DataView)
                    {
                        Common.CopyDataViewToClipboard(lcv.SourceCollection as DataView);
                    }
                }
            }
        }
        
        Controls.GadgetProperties.RatesProperties properties = null;

        public override void ShowHideConfigPanel()
        {
            Popup = new DashboardPopup();
            if (this.Parent is DragCanvas)
            {
                Popup.Parent = ((this.Parent as DragCanvas).Parent as ScrollViewer).Parent as Grid;
            }
            else
            {
                Popup.Parent = this.Parent as Grid;
            }
               properties = new Controls.GadgetProperties.RatesProperties(this.DashboardHelper, this, (RatesParameters)Parameters, StrataGridList, columnOrder);

               if (DashboardHelper.ResizedWidth != 0 & DashboardHelper.ResizedHeight != 0)
               {
                double i_StandardHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                double i_StandardWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                float f_HeightRatio = new float();
                float f_WidthRatio = new float();
                f_HeightRatio = (float)((float)DashboardHelper.ResizedHeight / (float)i_StandardHeight);
                f_WidthRatio = (float)((float)DashboardHelper.ResizedWidth / (float)i_StandardWidth);

                properties.Height = (Convert.ToInt32(i_StandardHeight * f_HeightRatio)) / 1.07;
                properties.Width = (Convert.ToInt32(i_StandardWidth * f_WidthRatio)) / 1.07;
            }
            else
            {
               properties.Width = (System.Windows.SystemParameters.PrimaryScreenWidth / 1.07);
               properties.Height = (System.Windows.SystemParameters.PrimaryScreenHeight/ 1.15);
            }

            properties.Cancelled += new EventHandler(properties_Cancelled);
            properties.ChangesAccepted += new EventHandler(properties_ChangesAccepted);
            Popup.Content = properties;
            Popup.Show();
         
        }

        public override void GadgetBase_SizeChanged(double width, double height)
        {
            double i_StandardHeight = System.Windows.SystemParameters.PrimaryScreenHeight; 
            double i_StandardWidth = System.Windows.SystemParameters.PrimaryScreenWidth; 
            float f_HeightRatio = new float();
            float f_WidthRatio = new float();
            f_HeightRatio = (float)((float)height / (float)i_StandardHeight);
            f_WidthRatio = (float)((float)width / (float)i_StandardWidth);

            if (properties == null)
                properties = new Controls.GadgetProperties.RatesProperties(this.DashboardHelper, this, (RatesParameters)Parameters, StrataGridList, columnOrder);

            properties.Height = (Convert.ToInt32(i_StandardHeight * f_HeightRatio)) / 1.07;
            properties.Width = (Convert.ToInt32(i_StandardWidth * f_WidthRatio)) / 1.07;
        }

        private void properties_ChangesAccepted(object sender, EventArgs e)
        {
            Controls.GadgetProperties.RatesProperties properties = Popup.Content as Controls.GadgetProperties.RatesProperties;
            this.Parameters = properties.Parameters;
            this.DataFilters = properties.DataFilters;
            this.CustomOutputHeading = Parameters.GadgetTitle;
            this.CustomOutputDescription = Parameters.GadgetDescription;
            Popup.Close();
            if (properties.HasSelectedFields)
            {
                RefreshResults();
            }
        }

        private void properties_Cancelled(object sender, EventArgs e)
        {
            Popup.Close();
        }

        /// <summary>
        /// Clears the gadget's output
        /// </summary>
        public void ClearResults()
        {
            messagePanel.Visibility = System.Windows.Visibility.Collapsed;
            messagePanel.Text = string.Empty;
            descriptionPanel.PanelMode = Controls.GadgetDescriptionPanel.DescriptionPanelMode.Collapsed;

            panelMain.Children.Clear();
        }
        #endregion // Public Methods

        private void AddDataGrid(DataView dv, string strataValue)
        {
            DataGrid dg = new DataGrid();
            dg.Style = this.Resources["LineListDataGridStyle"] as Style;
            //dg.BorderThickness = new Thickness(0);

            RatesParameters ListParameters = (this.Parameters) as RatesParameters;

            FrameworkElementFactory datagridRowsPresenter = new FrameworkElementFactory(typeof(DataGridRowsPresenter));
            ItemsPanelTemplate itemsPanelTemplate = new ItemsPanelTemplate();
            itemsPanelTemplate.VisualTree = datagridRowsPresenter;
            
            GroupStyle groupStyle = new GroupStyle();
            groupStyle.ContainerStyle = this.Resources["DefaultGroupItemStyle"] as Style;
            groupStyle.Panel = itemsPanelTemplate;
            dg.GroupStyle.Add(groupStyle);

            GroupStyle groupStyle2 = new GroupStyle();
            groupStyle2.HeaderTemplate = this.Resources["GroupDataTemplate"] as DataTemplate;
            dg.GroupStyle.Add(groupStyle2);

            string groupVar = String.Empty;

            if (IsHostedByEnter)
            {
                dv.Sort = "UniqueKey ASC";
            }

            DataTable dataTable = dv.ToTable();
            if (dataTable.Rows.Count > ListParameters.MaxRows && ListParameters.MaxRows > 0) //Added condition for EI-336
            {
                dataTable = dataTable.AsEnumerable().Skip(0).Take(ListParameters.MaxRows).CopyToDataTable();
            }

            //if (ListParameters.ShowLineColumn)
            //{
            //    DataColumn dc = new DataColumn();
            //    dc.ColumnName = "Line";
            //    dc.DataType = typeof(System.Int32);
            //    dataTable.Columns.Add(dc);
            //    dc.SetOrdinal(0);
            //    int rowid = 1;
            //    foreach (DataRow dr in dataTable.Rows)
            //    {
            //        dr["Line"] = rowid++;
            //    }
            //}            
                      
            DataView dataView = new DataView(dataTable);
            
            if (!String.IsNullOrEmpty(ListParameters.PrimaryGroupField.Trim()))
            {
                groupVar = ListParameters.PrimaryGroupField.Trim();
                ListCollectionView lcv = new ListCollectionView(dataView);
                lcv.GroupDescriptions.Add(new PropertyGroupDescription(groupVar));
                
                if (!String.IsNullOrEmpty(ListParameters.SecondaryGroupField.Trim()) && !ListParameters.SecondaryGroupField.Trim().Equals(groupVar))
                {
                    lcv.GroupDescriptions.Add(new PropertyGroupDescription(ListParameters.SecondaryGroupField.Trim())); // for second category
                }
                
                dg.ItemsSource = lcv;
            }
            else
            {
                dg.ItemsSource = dataView;
            }
            
            if (Parameters.Height.HasValue)
            {
                dg.MaxHeight = Parameters.Height.Value;
            }
            else
            {
                dg.MaxHeight = 700;
            }

            if (Parameters.Width.HasValue)
            {
                dg.MaxWidth = Parameters.Width.Value;
            }
            else
            {
                dg.MaxWidth = 900;
            }

            if (ListParameters.ShowColumnHeadings == false)
            {
                dg.ColumnHeaderHeight = 0;
            }
            
            //dg.AutoGeneratedColumns += new EventHandler(dg_AutoGeneratedColumns);
            //dg.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(dg_AutoGeneratingColumn);

            if (IsHostedByEnter)
            {
                Style rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new EventSetter(DataGridRow.MouseDoubleClickEvent, new MouseButtonEventHandler(Row_DoubleClick)));
                dg.RowStyle = rowStyle;
            }

            this.Height = dg.Height + 1024;

            panelMain.Children.Add(dg);
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            string CellValue = ((DataRow)((System.Data.DataRowView)(row.Item)).Row)["UniqueKey"].ToString();
            RecordSelected(Convert.ToInt32(CellValue));
        }

        #region Event Handlers

        /// <summary>
        /// Handles the WorkerCompleted event for the worker
        /// </summary>
        /// <param name="sender">Object that fired the event</param>
        /// <param name="e">.NET supplied event parameters</param>
        protected override void worker_WorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            Debug.Print("Background worker thread for rates gadget was cancelled or ran to completion.");
            this.Dispatcher.BeginInvoke(new SimpleCallback(SetGadgetToFinishedState));
        }

        /// <summary>
        /// Handles the DoWorker event for the worker
        /// </summary>
        /// <param name="sender">Object that fired the event</param>
        /// <param name="e">.NET supplied event parameters</param>
        protected override void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            lock (syncLock)
            {
                RatesParameters ratesParameters = null;
                if ((Parameters is RatesParameters) == true)
                {
                    ratesParameters = ((RatesParameters)Parameters);
                }
                else
                {
                    return;
                }

                String numerExpression = "([MosquitoesPresent] = 1) ";
                if (Parameters != null && ratesParameters.NumerFilter != null)
                {
                    //numerExpression = ratesParameters.NumerFilter.GenerateDataFilterString(false);
                }
                else
                {
                    //return;
                }

                String denomExpression = "([HouseID] is not null )";
                if (Parameters != null && ratesParameters.DenomFilter != null)
                {
                    //denomExpression = ratesParameters.DenomFilter.GenerateDataFilterString(false);
                }
                else
                {
                    //return;
                }

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                this.Dispatcher.BeginInvoke(new SimpleCallback(SetGadgetToProcessingState));
                this.Dispatcher.BeginInvoke(new SimpleCallback(ClearResults));
                AddDataGridDelegate addDataGrid = new AddDataGridDelegate(AddDataGrid);

                try
                {
                    int maxRows = ratesParameters.MaxRows;
                    bool exceededMaxRows = false;
                    Parameters.GadgetStatusUpdate += new GadgetStatusUpdateHandler(requestUpdateStatus);
                    Parameters.GadgetCheckForCancellation += new GadgetCheckForCancellationHandler(checkForCancellation);
                    if (this.DataFilters != null)
                    {
                        Parameters.CustomFilter = this.DataFilters.GenerateDataFilterString(false);
                    }
                    else
                    {
                        Parameters.CustomFilter = string.Empty;
                    }

                    ratesParameters.ColumnNames = new List<string>() { "MosquitoesPresent", "HouseID" };

                    List<DataTable> ratesTables = DashboardHelper.GenerateRates(ratesParameters);

                    string primaryGroupField = ratesParameters.PrimaryGroupField;

                    List<string> groupFields = new List<string>();
                    groupFields.Add(primaryGroupField);
                    DataTable groupTable = new DataTable();
                    List<string> primaryGroupElements = new List<string>();

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("Rate");
                    dataTable.Columns.Add("Rate_Description");
                    if (string.IsNullOrEmpty(primaryGroupField) == false)
                    {
                        dataTable.Columns.Add(((RatesParameters)Parameters).PrimaryGroupField);
                    }
                    DataRow newRow = dataTable.NewRow();

                    foreach (DataTable table in ratesTables)
                    {
                        groupTable = table.DefaultView.ToTable(true, groupFields.ToArray());

                        foreach (DataRow row in groupTable.Rows)
                        {
                            string aggregateName = groupTable.Columns[0].ColumnName;
                            string groupName = row[aggregateName] as string;
                            string aggregateExpression = "([" + aggregateName + "] = '" + groupName + "')";

                            string numerSelect = aggregateExpression + " AND " + denomExpression + " AND " + numerExpression;
                            string denomSelect = aggregateExpression + " AND " + denomExpression;                         

                            var numerRows = table.Select(numerSelect);
                            var denomRows = table.Select(denomSelect);

                            var distinctNumer = (from DataRow dRow in numerRows
                                                select new { col1 = dRow["MosquitoesPresent"], col2 = dRow["HouseID"] }).Distinct();

                            var distinctDenom = (from DataRow dRow in denomRows
                                                 select new { col1 = dRow["HouseID"] }).Distinct();

                            int numerValue = distinctNumer.Count(); 
                            int denomValue = distinctDenom.Count();

                            double rate = ((double)numerValue / (double)denomValue) * 100;

                            newRow = dataTable.NewRow();
                            newRow["Rate"] = rate;
                            newRow["Rate_Description"] = aggregateExpression;
                            newRow[primaryGroupField] = groupName;
                            dataTable.Rows.Add(newRow);
                        }
                    }

                    ratesTables = new List<DataTable>();
                    ratesTables.Add(dataTable);

                    if (ratesTables == null || ratesTables.Count == 0)
                    {
                        this.Dispatcher.BeginInvoke(new RenderFinishWithErrorDelegate(RenderFinishWithError), SharedStrings.NO_RECORDS_SELECTED);
                        this.Dispatcher.BeginInvoke(new SimpleCallback(SetGadgetToFinishedState));
                        Debug.Print("Rates thread cancelled");
                        return;
                    }
                    else if (ratesTables.Count == 1 && ratesTables[0].Rows.Count == 0)
                    {
                        this.Dispatcher.BeginInvoke(new RenderFinishWithErrorDelegate(RenderFinishWithError), SharedStrings.NO_RECORDS_SELECTED);
                        this.Dispatcher.BeginInvoke(new SimpleCallback(SetGadgetToFinishedState));
                        Debug.Print("Rates thread cancelled");
                        return;
                    }
                    else if (worker.CancellationPending)
                    {
                        this.Dispatcher.BeginInvoke(new RenderFinishWithErrorDelegate(RenderFinishWithError), SharedStrings.DASHBOARD_GADGET_STATUS_OPERATION_CANCELLED);
                        this.Dispatcher.BeginInvoke(new SimpleCallback(SetGadgetToFinishedState));
                        Debug.Print("Rates thread cancelled");
                        return;
                    }
                    else
                    {
                        string formatString = string.Empty;

                        foreach (DataTable listTable in ratesTables)
                        {
                            string strataValue = listTable.TableName;
                            if (listTable.Rows.Count == 0)
                            {
                                continue;
                            }
                        }

                        foreach (DataTable listTable in ratesTables)
                        {
                            string strataValue = listTable.TableName;
                            if (listTable.Rows.Count == 0)
                            {
                                continue;
                            }
                            string tableHeading = listTable.TableName;
                            if (Parameters.ColumnNames.Count != columnOrder.Count)
                                columnOrder = new List<string>();
                            SetCustomColumnSort(listTable);

                            if (listTable.Columns.Count == 0)
                            {
                                throw new ApplicationException("There are no columns to display in this list. If specifying a group variable, ensure the group variable contains data fields.");
                            }

                            int[] totals = new int[listTable.Columns.Count - 1];
                            if (listTable.Rows.Count > maxRows)
                            {
                                exceededMaxRows = true;
                            } 
                            this.Dispatcher.BeginInvoke(addDataGrid, listTable.AsDataView(), listTable.TableName);                            
                        }                        
                    }
                    if (exceededMaxRows)
                    {
                        this.Dispatcher.BeginInvoke(new RenderFinishWithWarningDelegate(RenderFinishWithWarning), string.Format(SharedStrings.DASHBOARD_GADGET_STATUS_ROW_LIMIT, maxRows.ToString()));                       
                    }
                    else

                    this.Dispatcher.BeginInvoke(new SimpleCallback(RenderFinish));
                }
                catch (Exception ex)
                {
                    this.Dispatcher.BeginInvoke(new RenderFinishWithErrorDelegate(RenderFinishWithError), ex.Message);
                }
                finally
                {
                    stopwatch.Stop();
                    Debug.Print("Rates gadget took " + stopwatch.Elapsed.ToString() + " seconds to complete with " + DashboardHelper.RecordCount.ToString() + " records and the following filters:");
                    Debug.Print(DashboardHelper.DataFilters.GenerateDataFilterString());
                }
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Used to construct the gadget and assign events
        /// </summary>
        /// <param name="dashboardHelper">The dashboard helper to attach to this gadget</param>
        protected override void Construct()
        {
            currentWidth = borderAll.ActualWidth;
            this.Parameters = new RatesParameters();

            ((EpiDashboard.RatesParameters)(Parameters)).MaxRows = 50;
            ((EpiDashboard.RatesParameters)(Parameters)).MaxColumnLength = 24;
            if (!string.IsNullOrEmpty(CustomOutputHeading))
            {
                headerPanel.Text = CustomOutputHeading;
            }

            requestUpdateStatus = new RequestUpdateStatusDelegate(RequestUpdateStatusMessage);
            checkForCancellation = new CheckForCancellationDelegate(IsCancelled);

            mnuCopy.Click += new RoutedEventHandler(mnuCopy_Click);
            mnuSendDataToHTML.Click += new RoutedEventHandler(mnuSendDataToHTML_Click);
            mnuRemoveSorts.Click += mnuRemoveSorts_Click;

#if LINUX_BUILD
            mnuSendDataToExcel.Visibility = Visibility.Collapsed;
#else
            mnuSendDataToExcel.Visibility = Visibility.Visible;
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot; Microsoft.Win32.RegistryKey excelKey = key.OpenSubKey("Excel.Application"); bool excelInstalled = excelKey == null ? false : true; key = Microsoft.Win32.Registry.ClassesRoot;
            excelKey = key.OpenSubKey("Excel.Application");
            excelInstalled = excelKey == null ? false : true;

            if (!excelInstalled)
            {
                mnuSendDataToExcel.Visibility = Visibility.Collapsed;
            }
            else
            {
                mnuSendDataToExcel.Click += new RoutedEventHandler(mnuSendDataToExcel_Click);
            }
#endif
            if (!IsHostedByEnter)
            {
                mnuSendToBack.Click += new RoutedEventHandler(mnuSendToBack_Click);
                mnuClose.Click += new RoutedEventHandler(mnuClose_Click);
                mnuCenter.Click += new RoutedEventHandler(mnuCenter_Click);
                mnuSendDataToHTML.Visibility = System.Windows.Visibility.Visible;
                mnuClose.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                mnuSendDataToHTML.Visibility = System.Windows.Visibility.Collapsed;
                mnuClose.Visibility = System.Windows.Visibility.Collapsed;
                mnuCenter.Visibility = System.Windows.Visibility.Collapsed;
                borderAll.ContextMenu = null;
                headerPanel.IsCloseButtonAvailable = false;
                headerPanel.IsDescriptionButtonAvailable = false;
                headerPanel.IsFilterButtonAvailable = false;
                headerPanel.IsOutputCollapseButtonAvailable = false;
            }

            this.IsProcessing = true;

            this.GadgetStatusUpdate += new GadgetStatusUpdateHandler(RequestUpdateStatusMessage);
            this.GadgetCheckForCancellation += new GadgetCheckForCancellationHandler(IsCancelled);

            base.Construct();

            #region Translation
            mnuCenter.Header = DashboardSharedStrings.GADGET_CENTER;
            mnuClose.Header = LineListSharedStrings.CMENU_CLOSE;
            mnuCopy.Header = LineListSharedStrings.CMENU_COPY;
            mnuRemoveSorts.Header = LineListSharedStrings.CMENU_REMOVE_SORTING;
            mnuSendDataToExcel.Header = LineListSharedStrings.CMENU_EXCEL;
            mnuSendDataToHTML.Header = LineListSharedStrings.CMENU_HTML;
            mnuSendToBack.Header = LineListSharedStrings.CMENU_SEND_TO_BACK;
            #endregion // Translation
        }

        void mnuRemoveSorts_Click(object sender, RoutedEventArgs e)
        {
            if(properties!=null)
            {
                Parameters.SortVariables.Clear();
                RefreshResults();                         
            }
        }

        void mnuCenter_Click(object sender, RoutedEventArgs e)
        {
            CenterGadget();
        }

        /// <summary>
        /// Closes the gadget
        /// </summary>
        protected override void CloseGadget()
        {
            if (worker != null && worker.WorkerSupportsCancellation)
            {
                worker.CancelAsync();
            }

            if (worker != null)
            {
                worker.DoWork -= new System.ComponentModel.DoWorkEventHandler(worker_DoWork);
                worker.RunWorkerCompleted -= new System.ComponentModel.RunWorkerCompletedEventHandler(worker_WorkerCompleted);
            }
            if (baseWorker != null)
            {
                baseWorker.DoWork -= new System.ComponentModel.DoWorkEventHandler(Execute);
            }

            if (Parameters != null)
            {
                Parameters.GadgetStatusUpdate -= new GadgetStatusUpdateHandler(requestUpdateStatus);
                Parameters.GadgetCheckForCancellation -= new GadgetCheckForCancellationHandler(checkForCancellation);
                Parameters = null;
            }

            this.GadgetStatusUpdate -= new GadgetStatusUpdateHandler(RequestUpdateStatusMessage);
            this.GadgetCheckForCancellation -= new GadgetCheckForCancellationHandler(IsCancelled);

            panelMain.Children.Clear();

            requestUpdateStatus = null;
            checkForCancellation = null;

            if (!IsHostedByEnter)
            {
                mnuSendToBack.Click -= new RoutedEventHandler(mnuSendToBack_Click);
                mnuClose.Click -= new RoutedEventHandler(mnuClose_Click);
            }

            base.CloseGadget();

            Parameters = null;
        }

        private void SetCustomColumnSort(DataTable table)
        {
            #region Input Validation
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }
            #endregion // Input Validation

            if (columnOrder == null || columnOrder.Count == 0)
            {
                return;
            }

            if (columnOrder.Contains("Rates"))
            {
                columnOrder.Remove("Rates");
            }

            for (int i = 0; i < columnOrder.Count; i++)
            {
                string s = columnOrder[i];
                if (table.Columns.Contains(s))
                {
                    table.Columns[s].SetOrdinal(i);
                }
            }
        }

        /// <summary>
        /// Sets the gadget's state to 'finished' mode
        /// </summary>
        protected override void RenderFinish()
        {
            waitPanel.Visibility = System.Windows.Visibility.Collapsed;

            messagePanel.MessagePanelType = Controls.MessagePanelType.StatusPanel;
            messagePanel.Text = string.Empty;
            messagePanel.Visibility = System.Windows.Visibility.Collapsed;

            HideConfigPanel();
            CheckAndSetPosition();
        }

        protected override void RenderFinishWithWarning(string errorMessage)
        {
            waitPanel.Visibility = System.Windows.Visibility.Collapsed;

            messagePanel.MessagePanelType = Controls.MessagePanelType.WarningPanel;
            messagePanel.Text = errorMessage;
            messagePanel.Visibility = System.Windows.Visibility.Visible;

            HideConfigPanel();
            CheckAndSetPosition();
        }

        /// <summary>
        /// Sets the gadget's state to 'finished with error' mode
        /// </summary>
        /// <param name="errorMessage">The error message to display</param>
        protected override void RenderFinishWithError(string errorMessage)
        {
            waitPanel.Visibility = System.Windows.Visibility.Collapsed;

            messagePanel.MessagePanelType = Controls.MessagePanelType.ErrorPanel;
            messagePanel.Text = errorMessage;
            messagePanel.Visibility = System.Windows.Visibility.Visible;

            HideConfigPanel();
            CheckAndSetPosition();
        }
        #endregion

        #region Private Properties
        private View View
        {
            get
            {
                return this.DashboardHelper.View;
            }
        }

        private IDbDriver Database
        {
            get
            {
                return this.DashboardHelper.Database;
            }
        }
        #endregion // Private Properties

        #region IGadget Members

        public override void CollapseOutput()
        {
            borderAll.MinWidth = currentWidth;
            panelMain.Visibility = System.Windows.Visibility.Collapsed;

            this.messagePanel.Visibility = System.Windows.Visibility.Collapsed;
            this.infoPanel.Visibility = System.Windows.Visibility.Collapsed;
            IsCollapsed = true;
        }

        public override void ExpandOutput()
        {
            if (this.messagePanel.MessagePanelType != Controls.MessagePanelType.StatusPanel)
            {
                this.messagePanel.Visibility = System.Windows.Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(this.infoPanel.Text))
            {
                this.infoPanel.Visibility = System.Windows.Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(this.descriptionPanel.Text))
            {
                descriptionPanel.PanelMode = Controls.GadgetDescriptionPanel.DescriptionPanelMode.DisplayMode;
            }

            panelMain.Visibility = System.Windows.Visibility.Visible;

            IsCollapsed = false;
        }

        protected override void ShowCustomFilterDialog()
        {
            base.ShowCustomFilterDialog();
        }

        protected override void SetCustomFilterDisplay()
        {
            if (this.DataFilters != null && this.DataFilters.Count > 0)
            {
                infoPanel.MaxWidth = this.ActualWidth;
                infoPanel.Text = String.Format(DashboardSharedStrings.GADGET_CUSTOM_FILTER, this.DataFilters.GenerateReadableDataFilterString());
                infoPanel.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                infoPanel.Text = string.Empty;
                infoPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Initiates a refresh of the gadget's output
        /// </summary>
        public override void RefreshResults()
        {
            if (!LoadingCombos && Parameters != null && Parameters.ColumnNames.Count > 0)
            {
                if (IsHostedByEnter)
                {
                    HideConfigPanel();
                }
                waitPanel.Visibility = System.Windows.Visibility.Visible;

                messagePanel.MessagePanelType = Controls.MessagePanelType.StatusPanel;
                descriptionPanel.PanelMode = Controls.GadgetDescriptionPanel.DescriptionPanelMode.Collapsed;

                baseWorker = new BackgroundWorker();
                baseWorker.WorkerSupportsCancellation = true;
                baseWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(Execute);
                baseWorker.RunWorkerAsync();

                base.RefreshResults();
            }
            else
            {
                return;
            }

            base.RefreshResults();
        }

        /// <summary>
        /// Updates the variable names available in the gadget's properties
        /// </summary>
        public override void UpdateVariableNames()
        {
            //FillComboboxes(true);
        }

        /// <summary>
        /// Generates Xml representation of this gadget
        /// </summary>
        /// <param name="doc">The Xml docment</param>
        /// <returns>XmlNode</returns>
        public override XmlNode Serialize(XmlDocument doc)
        {
            RatesParameters ratesParameters = (RatesParameters)Parameters;

            System.Xml.XmlElement element = doc.CreateElement("ratesGadget");
            element.AppendChild(SerializeFilters(doc));

            System.Xml.XmlAttribute id = doc.CreateAttribute("id");
            System.Xml.XmlAttribute locationY = doc.CreateAttribute("top");
            System.Xml.XmlAttribute locationX = doc.CreateAttribute("left");
            System.Xml.XmlAttribute collapsed = doc.CreateAttribute("collapsed");
            System.Xml.XmlAttribute type = doc.CreateAttribute("gadgetType");
            System.Xml.XmlAttribute actualHeight = doc.CreateAttribute("actualHeight");

            id.Value = this.UniqueIdentifier.ToString();
            locationY.Value = Canvas.GetTop(this).ToString("F0");
            locationX.Value = Canvas.GetLeft(this).ToString("F0");
            collapsed.Value = IsCollapsed.ToString();
            type.Value = "EpiDashboard.RatesControl";
            actualHeight.Value = this.ActualHeight.ToString();

            element.Attributes.Append(locationY);
            element.Attributes.Append(locationX);
            element.Attributes.Append(collapsed);
            element.Attributes.Append(type);
            element.Attributes.Append(id);

            if (IsCollapsed == false)
            {
                element.Attributes.Append(actualHeight);
            }

            string numerVar = String.Empty;
            string denomVar = String.Empty;

            if (!String.IsNullOrEmpty(ratesParameters.NumeratorField))
            {
                numerVar = ratesParameters.NumeratorField;
            }

            if (!String.IsNullOrEmpty(ratesParameters.DenominatorField))
            {
                denomVar = ratesParameters.DenominatorField;
            }

            XmlElement numerElement = doc.CreateElement("numerVariable");
            numerElement.InnerText = numerVar;
            element.AppendChild(numerElement);

            XmlElement denomElement = doc.CreateElement("denomVariable");
            denomElement.InnerText = denomVar;
            element.AppendChild(denomElement);

            string groupVar1 = String.Empty;
            string groupVar2 = String.Empty;

            if (!String.IsNullOrEmpty(ratesParameters.PrimaryGroupField))
            {
                groupVar1 = ratesParameters.PrimaryGroupField;
            }
            if (!String.IsNullOrEmpty(ratesParameters.SecondaryGroupField))
            {
                groupVar2 = ratesParameters.SecondaryGroupField;
            }

            XmlElement groupElement = doc.CreateElement("groupVariable");
            groupElement.InnerText = groupVar1;
            element.AppendChild(groupElement);

            XmlElement groupElement2 = doc.CreateElement("groupVariableSecondary");
            groupElement2.InnerText = groupVar2;
            element.AppendChild(groupElement2);

            XmlElement tabOrderElement = doc.CreateElement("sortColumnsByTabOrder");
            tabOrderElement.InnerText = ratesParameters.SortColumnsByTabOrder.ToString();
            element.AppendChild(tabOrderElement);

            XmlElement usePromptsElement = doc.CreateElement("usepromptsforcolumnnames");
            usePromptsElement.InnerText = ratesParameters.UsePromptsForColumnNames.ToString();
            element.AppendChild(usePromptsElement);

            XmlElement showListLabelsElement = doc.CreateElement("showListLabels");
            showListLabelsElement.InnerText = ratesParameters.ShowCommentLegalLabels.ToString();
            element.AppendChild(showListLabelsElement);

            XmlElement showLineColumnElement = doc.CreateElement("showLineColumn");
            showLineColumnElement.InnerText = ratesParameters.ShowLineColumn.ToString();
            element.AppendChild(showLineColumnElement);

            XmlElement showColumnHeadersElement = doc.CreateElement("showColumnHeadings");
            showColumnHeadersElement.InnerText = ratesParameters.ShowColumnHeadings.ToString();
            element.AppendChild(showColumnHeadersElement);

            XmlElement showNullLabelsElement = doc.CreateElement("showNullLabels");
            showNullLabelsElement.InnerText = ratesParameters.ShowNullLabels.ToString();
            element.AppendChild(showNullLabelsElement);

            XmlElement showAsPercentElement = doc.CreateElement("showAsPercent");
            showNullLabelsElement.InnerText = ratesParameters.ShowNullLabels.ToString();
            element.AppendChild(showNullLabelsElement);

            XmlElement numerDistinctElement = doc.CreateElement("numerDistinct");
            numerDistinctElement.InnerText = ratesParameters.NumerDistinct.ToString();
            element.AppendChild(numerDistinctElement);

            XmlElement denomDistinctElement = doc.CreateElement("denomDistinct");
            denomDistinctElement.InnerText = ratesParameters.DenomDistinct.ToString();
            element.AppendChild(denomDistinctElement);

            XmlElement customHeadingElement = doc.CreateElement("customHeading");
            customHeadingElement.InnerText = ratesParameters.GadgetTitle; // CustomOutputHeading.Replace("<", "&lt;");
            element.AppendChild(customHeadingElement);

            XmlElement customDescElement = doc.CreateElement("customDescription");
            customDescElement.InnerText = ratesParameters.GadgetDescription; // CustomOutputDescription.Replace("<", "&lt;");
            element.AppendChild(customDescElement);

            XmlElement customMaxRowElement = doc.CreateElement("maxRows");
            customMaxRowElement.InnerText = ratesParameters.MaxRows.ToString(); // CustomOutputHeading.Replace("<", "&lt;");
            element.AppendChild(customMaxRowElement);

            XmlElement customMaxColumnElement = doc.CreateElement("maxColumnNameLength");
            customMaxColumnElement.InnerText = ratesParameters.MaxColumnLength.ToString(); // CustomOutputHeading.Replace("<", "&lt;");
            element.AppendChild(customMaxColumnElement);

            if (ratesParameters.Width != null)
            {
                XmlElement customMaxWidthElement = doc.CreateElement("maxWidth");
                customMaxWidthElement.InnerText = ratesParameters.Width.ToString(); // CustomOutputHeading.Replace("<", "&lt;");
                element.AppendChild(customMaxWidthElement);
            }

            if (ratesParameters.Height != null)
            {
                XmlElement customMaxHeightElement = doc.CreateElement("maxHeight");
                customMaxHeightElement.InnerText = ratesParameters.Height.ToString(); // CustomOutputHeading.Replace("<", "&lt;");
                element.AppendChild(customMaxHeightElement);
            }

            SerializeAnchors(element);

            if (!string.IsNullOrEmpty(ratesParameters.CustomSortColumnName))
            {
                XmlElement customusercolumnsort = doc.CreateElement("customusercolumnsort");
                customusercolumnsort.InnerText = ratesParameters.CustomSortColumnName;
                element.AppendChild(customusercolumnsort);
            }
            else if (columnOrder != null && columnOrder.Count > 0) // when user has re-ordered columns but not refreshed
            {
                XmlElement customusercolumnsort = doc.CreateElement("customusercolumnsort");
                WordBuilder wb = new WordBuilder("^");
                for (int i = 0; i < columnOrder.Count; i++)
                {
                    wb.Add(columnOrder[i]);
                }
                customusercolumnsort.InnerText = wb.ToString();
                element.AppendChild(customusercolumnsort);
            }

            XmlElement listItemElement = doc.CreateElement("listFields");

            string xmlListItemString = string.Empty;

            foreach (string columnName in ratesParameters.ColumnNames)
            {
                xmlListItemString = xmlListItemString + "<listField>" + columnName.Replace("<", "&lt;") + "</listField>";
            }

            listItemElement.InnerXml = xmlListItemString;

            if (!String.IsNullOrEmpty(xmlListItemString))
            {
                element.AppendChild(listItemElement);
            }

            XmlElement sortItemElement = doc.CreateElement("sortFields");

            string xmlSortItemString = string.Empty;

            foreach (KeyValuePair<string, SortOrder> kvp in Parameters.SortVariables)
            {
                if (kvp.Value == SortOrder.Descending)
                {
                    xmlSortItemString = xmlSortItemString + "<sortField order=\"DESC\">" + kvp.Key.Replace("<", "&lt;") + "</sortField>";
                }
                else
                {
                    xmlSortItemString = xmlSortItemString + "<sortField order=\"ASC\">" + kvp.Key.Replace("<", "&lt;") + "</sortField>";
                }
            }

            sortItemElement.InnerXml = xmlSortItemString;

            if (!String.IsNullOrEmpty(xmlSortItemString))
            {
                element.AppendChild(sortItemElement);
            }

            if(ratesParameters.NumerFilter != null)
            {
                ratesParameters.NumerFilter.Serialize(doc);
            }

            if (ratesParameters.DenomFilter != null)
            {
                ratesParameters.DenomFilter.Serialize(doc);
            }

            return element;
        }

        /// <summary>
        /// Creates the rates gadget from an Xml element
        /// </summary>
        /// <param name="element">The element from which to create the gadget</param>
        public override void CreateFromXml(XmlElement element)
        {
            this.LoadingCombos = true;
            this.Parameters = new RatesParameters();

            HideConfigPanel();

            infoPanel.Visibility = System.Windows.Visibility.Collapsed;
            messagePanel.Visibility = System.Windows.Visibility.Collapsed;

            foreach (XmlElement child in element.ChildNodes)
            {
                switch (child.Name.ToLowerInvariant())
                {
                    case "groupvariable":
                    case "groupvariableprimary":
                        if (!String.IsNullOrEmpty(child.InnerText.Trim()))
                        {
                            ((RatesParameters)Parameters).PrimaryGroupField = child.InnerText.Trim();
                        }
                        break;
                    case "groupvariablesecondary":
                        if (!String.IsNullOrEmpty(child.InnerText.Trim()))
                        {
                            ((RatesParameters)Parameters).SecondaryGroupField = child.InnerText.Trim();
                        }
                        break;
                    case "numerVariable":
                        if (!String.IsNullOrEmpty(child.InnerText.Trim()))
                        {
                            ((RatesParameters)Parameters).NumeratorField = child.InnerText.Trim();
                        }
                        break;
                    case "denomVariable":
                        if (!String.IsNullOrEmpty(child.InnerText.Trim()))
                        {
                            ((RatesParameters)Parameters).DenominatorField = child.InnerText.Trim();
                        }
                        break;
                    case "maxcolumnnamelength":
                        int maxColumnLength = 24;
                        int.TryParse(child.InnerText, out maxColumnLength);
                        ((RatesParameters)Parameters).MaxColumnLength = maxColumnLength;
                        break;
                    case "maxrows":
                        int maxRows = 50;
                        int.TryParse(child.InnerText, out maxRows);
                        ((RatesParameters)Parameters).MaxRows = maxRows;
                        break;
                    case "sortcolumnsbytaborder":
                        bool sortByTabs = false;
                        bool.TryParse(child.InnerText, out sortByTabs);
                        ((RatesParameters)Parameters).SortColumnsByTabOrder = sortByTabs;
                        break;
                    case "usepromptsforcolumnnames":
                        bool usePrompts = false;
                        bool.TryParse(child.InnerText, out usePrompts);
                        ((RatesParameters)Parameters).UsePromptsForColumnNames = usePrompts;
                        break;
                    case "showlinecolumn":
                        bool showLineColumn = true;
                        bool.TryParse(child.InnerText, out showLineColumn);
                        ((RatesParameters)Parameters).ShowLineColumn = showLineColumn;
                        break;
                    case "showcolumnheadings":
                        bool showColumnHeadings = true;
                        bool.TryParse(child.InnerText, out showColumnHeadings);
                        ((RatesParameters)Parameters).ShowColumnHeadings = showColumnHeadings;
                        break;
                    case "showlistlabels":
                        bool showLabels = false;
                        bool.TryParse(child.InnerText, out showLabels);
                        Parameters.ShowCommentLegalLabels = showLabels;
                        break;
                    case "shownulllabels":
                        bool showNullLabels = true;
                        bool.TryParse(child.InnerText, out showNullLabels);
                        ((RatesParameters)Parameters).ShowNullLabels = showNullLabels;
                        break;
                    case "showaspercent":
                        bool showAsPercent = true;
                        bool.TryParse(child.InnerText, out showAsPercent);
                        ((RatesParameters)Parameters).ShowAsPercent = showAsPercent;
                        break;
                    case "numerDistinct":
                        bool numerDistinct = true;
                        bool.TryParse(child.InnerText, out numerDistinct);
                        ((RatesParameters)Parameters).NumerDistinct = numerDistinct;
                        break;
                    case "denomDistinct":
                        bool denomDistinct = true;
                        bool.TryParse(child.InnerText, out denomDistinct);
                        ((RatesParameters)Parameters).DenomDistinct = denomDistinct;
                        break;
                    case "rateMultiplyer":
                        int rateMultiplyer = 100;
                        int.TryParse(child.InnerText, out rateMultiplyer);
                        ((RatesParameters)Parameters).RateMultiplier = rateMultiplyer;
                        break;
                    case "customheading":
                        if (!string.IsNullOrEmpty(child.InnerText))
                        {
                            this.CustomOutputHeading = child.InnerText.Replace("&lt;", "<");
                            Parameters.GadgetTitle = CustomOutputHeading;
                        }
                        break;
                    case "customdescription":
                        if (!string.IsNullOrEmpty(child.InnerText))
                        {
                            this.CustomOutputDescription = child.InnerText.Replace("&lt;", "<");
                            Parameters.GadgetDescription = CustomOutputDescription;
                            if (!string.IsNullOrEmpty(CustomOutputDescription) && !CustomOutputHeading.Equals("(none)"))
                            {
                                descriptionPanel.Text = CustomOutputDescription;
                                descriptionPanel.PanelMode = Controls.GadgetDescriptionPanel.DescriptionPanelMode.DisplayMode;
                            }
                            else
                            {
                                descriptionPanel.PanelMode = Controls.GadgetDescriptionPanel.DescriptionPanelMode.Collapsed;
                            }
                        }
                        break;
                    case "customcaption":
                        this.CustomOutputCaption = child.InnerText;
                        break;
                    case "listfields":
                        foreach (XmlElement field in child.ChildNodes)
                        {
                            List<string> fields = new List<string>();
                            if (field.Name.ToLowerInvariant().Equals("listfield"))
                            {
                                //GadgetOptions.InputVariableList.Add(field.InnerText.Replace("&lt;", "<"), "listfield");
                                Parameters.ColumnNames.Add(field.InnerText.Replace("&lt;", "<"));
                            }
                        }
                        break;
                    case "sortfields":
                        foreach (XmlElement field in child.ChildNodes)
                        {
                            List<string> fields = new List<string>();

                            SortOrder order = SortOrder.Ascending;

                            if (field.Attributes.Count >= 1 && field.Attributes["order"].Value == "DESC")
                            {
                                order = SortOrder.Descending;
                            }

                            if (field.Name.ToLowerInvariant().Equals("sortfield"))
                            {
                                if (field.Attributes["order"]==null)
                                {
                                    if (field.InnerText.Contains("(ascending)"))
                                        Parameters.SortVariables.Add(field.InnerText.Replace("&lt;", "<").Substring(0, field.InnerText.Replace("&lt;", "<").IndexOf("(") - 1), SortOrder.Ascending);
                                    else Parameters.SortVariables.Add(field.InnerText.Replace("&lt;", "<").Substring(0, field.InnerText.Replace("&lt;", "<").IndexOf("(") - 1), SortOrder.Descending);
                                }
                                else
                                    Parameters.SortVariables.Add(field.InnerText.Replace("&lt;", "<"), order);
                            }
                        }
                        break;
                    case "customusercolumnsort":
                        string[] cols = child.InnerText.Split('^');
                        columnOrder = cols.ToList();
                        break;
                    case "datafilters":
                        this.DataFilters = new DataFilters(this.DashboardHelper);
                        this.DataFilters.CreateFromXml(child);
                        break;
                    case "maxheight":
                        int maxHeight = 500;
                        int.TryParse(child.InnerText, out maxHeight);
                        ((RatesParameters)Parameters).Height = maxHeight;
                        break;
                    case "maxwidth":
                        int maxWidth = 800;
                        int.TryParse(child.InnerText, out maxWidth);
                        ((RatesParameters)Parameters).Width = maxWidth;
                        break;
                }
            }

            base.CreateFromXml(element);

            this.LoadingCombos = false;

            RefreshResults();
            HideConfigPanel();
        }

        #endregion

        /// <summary>
        /// Sets the gadget to its 'processing' state
        /// </summary>
        public override void SetGadgetToProcessingState()
        {
            this.IsProcessing = true;
            base.SetGadgetToProcessingState();
        }

        /// <summary>
        /// Sets the gadget to its 'finished' state
        /// </summary>
        public override void SetGadgetToFinishedState()
        {
            this.IsProcessing = false;
            currentWidth = borderAll.ActualWidth;
            base.SetGadgetToFinishedState();
        }

        /// <summary>
        /// Converts the gadget's output to Html
        /// </summary>
        /// <returns></returns>
        public override string ToHTML(string htmlFileName = "", int count = 0, bool useAlternatingColors = false)
        {
            if (IsCollapsed) return string.Empty;
            String groupField = (Parameters as EpiDashboard.RatesParameters).PrimaryGroupField;
            StringBuilder htmlBuilder = new StringBuilder();
            CustomOutputHeading = headerPanel.Text;
            CustomOutputDescription = descriptionPanel.Text;

            if (CustomOutputHeading == null || (string.IsNullOrEmpty(CustomOutputHeading) && !CustomOutputHeading.Equals("(none)")))
            {
                htmlBuilder.AppendLine("<h2 class=\"gadgetHeading\">Rates</h2>");
            }
            else if (CustomOutputHeading != "(none)")
            {
                htmlBuilder.AppendLine("<h2 class=\"gadgetHeading\">" + CustomOutputHeading + "</h2>");
            }

            if (!string.IsNullOrEmpty(CustomOutputDescription) && CustomOutputDescription != "(none)")
            {
                htmlBuilder.AppendLine("<p class=\"gadgetsummary\">" + CustomOutputDescription + "</p>");
            }

            if (!string.IsNullOrEmpty(messagePanel.Text) && messagePanel.Visibility == Visibility.Visible)
            {
                htmlBuilder.AppendLine("<p><small><strong>" + messagePanel.Text + "</strong></small></p>");
            }

            DataGrid dg = GetDataGrid();
            Dictionary<String, bool> groupValues = new Dictionary<String, bool>();

            if (dg != null && dg.ItemsSource != null)
            {
                if (!String.IsNullOrEmpty(groupField))
                {
                    if (dg.ItemsSource is DataView)
                    {
                        DataView idv = dg.ItemsSource as DataView;
                        foreach (DataRow dr in idv.Table.Rows)
                        {
                            if (!String.IsNullOrEmpty(dr[groupField].ToString() as String) && !groupValues.Keys.Contains(dr[groupField].ToString() as String))
                            {
                                groupValues.Add(dr[groupField].ToString() as String, true);
                            }
                            else if (String.IsNullOrEmpty(dr[groupField].ToString() as String) && !groupValues.Keys.Contains(""))
                            {
                                groupValues.Add("", true);
                            }
                        }
                    }
                    else if (dg.ItemsSource is ListCollectionView)
                    {
                        ListCollectionView lcv = dg.ItemsSource as ListCollectionView;
                        if (lcv.SourceCollection is DataView)
                        {
                            DataView idv = lcv.SourceCollection as DataView;
                            foreach (DataRow dr in idv.Table.Rows)
                            {
                                if (!String.IsNullOrEmpty(dr[groupField].ToString() as String) && !groupValues.Keys.Contains(dr[groupField].ToString() as String))
                                {
                                    groupValues.Add(dr[groupField].ToString() as String, true);
                                }
                                else if (String.IsNullOrEmpty(dr[groupField].ToString() as String) && !groupValues.Keys.Contains(""))
                                {
                                    groupValues.Add("", true);
                                }
                            }
                        }
                    }
                }
                if (groupValues.Count == 0)
                {
                    groupValues.Add("", true);
                }
                foreach (String groupValue in groupValues.Keys)
                {
                    htmlBuilder.AppendLine("<div style=\"height: 7px;\"></div>");
                    if (!String.IsNullOrEmpty(groupField))
                    {
                        htmlBuilder.AppendLine("<h3>" + groupField + " = " + groupValue + "</h3>");
                    }
                    htmlBuilder.AppendLine("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">");
                    if (string.IsNullOrEmpty(CustomOutputCaption) && this.StrataGridList.Count > 1)
                    {
                        //htmlBuilder.AppendLine("<caption>" + grid.Tag + "</caption>");
                    }
                    else if (!string.IsNullOrEmpty(CustomOutputCaption))
                    {
                        htmlBuilder.AppendLine("<caption>" + CustomOutputCaption + "</caption>");
                    }

                    if (dg.ItemsSource is DataView)
                    {
                        if (!String.IsNullOrEmpty(groupField))
                        {
                            DataView sdv = dg.ItemsSource as DataView;
                            if (groupValue.Equals(""))
                                sdv.RowFilter = groupField + " is null";
                            else
                                sdv.RowFilter = groupField + " = '" + groupValue + "'";
                            DataTable sdvTable = sdv.ToTable();
                            if (!Parameters.ColumnNames.Contains(groupField))
                            {
                                sdvTable.Columns.Remove(groupField);
                            }
                            htmlBuilder.AppendLine(Common.ConvertDataViewToHtmlString(sdvTable.DefaultView, useAlternatingColors));
                        }
                        else
                        {
                            if (dg.ItemsSource is DataView)
                            {
                                DataView dgItemSource = dg.ItemsSource as DataView;
                                int dgtcindex = 0;
                                foreach (DataColumn dc in dgItemSource.Table.Columns)
                                {
                                    string nombre = dg.Columns[dgtcindex].Header.ToString();
                                    if (dg.Columns.Count > dgtcindex)
                                    {
                                        for (int i = 2; i < 24; i++)
                                        {
                                            if (dgItemSource.Table.Columns.Contains(nombre) && dc.Ordinal != 0)
                                            {
                                                nombre = nombre + '(' + i + ')';
                                            }
                                            else
                                            {
                                                dc.ColumnName = nombre;
                                                break;
                                            }
                                        }
                                    }
                                    dgtcindex++;
                                }

                                htmlBuilder.AppendLine(Common.ConvertDataViewToHtmlString(dg.ItemsSource as DataView, useAlternatingColors));
                            }
                        }
                    }
                    else if (dg.ItemsSource is ListCollectionView)
                    {
                        ListCollectionView lcv = dg.ItemsSource as ListCollectionView;
                        if (lcv.SourceCollection is DataView)
                        {
                            if (!String.IsNullOrEmpty(groupField))
                            {
                                DataView sdv = lcv.SourceCollection as DataView;
                                if (groupValue.Equals(""))
                                    sdv.RowFilter = groupField + " is null";
                                else
                                    sdv.RowFilter = groupField + " = '" + groupValue + "'";
                                DataTable sdvTable = sdv.ToTable();
                                if (!Parameters.ColumnNames.Contains(groupField))
                                {
                                    sdvTable.Columns.Remove(groupField);
                                }
                                htmlBuilder.AppendLine(Common.ConvertDataViewToHtmlString(sdvTable.DefaultView, useAlternatingColors));
                            }
                            else
                            {
                                htmlBuilder.AppendLine(Common.ConvertDataViewToHtmlString(lcv.SourceCollection as DataView, useAlternatingColors));
                            }
                        }
                    }

                    htmlBuilder.AppendLine("</table>");
                }
            }

            return htmlBuilder.ToString();
        }

        private string customOutputHeading;
        private string customOutputDescription;
        private string customOutputCaption;

        public override string CustomOutputHeading
        {
            get
            {
                return this.customOutputHeading;
            }
            set
            {
                this.customOutputHeading = value;
                headerPanel.Text = CustomOutputHeading;
            }
        }

        public override string CustomOutputDescription
        {
            get
            {
                return this.customOutputDescription;
            }
            set
            {
                this.customOutputDescription = value;
                descriptionPanel.Text = CustomOutputDescription;
            }
        }

        public override string CustomOutputCaption
        {
            get
            {
                return this.customOutputCaption;
            }
            set
            {
                this.customOutputCaption = value;
            }
        }

        public bool IsHostedByEnter
        {
            get
            {
                return isHostedByEnter;
            }
            set
            {
                isHostedByEnter = value;
            }
        }
    }
}
