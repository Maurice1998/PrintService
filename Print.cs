using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using ZXing.Common;

namespace HttpPrint
{
    public class PrintJobModel
    {
        public long job_id { get; set; }
        /// <summary>
        /// label key
        /// </summary>
        public string label_id { get; set; }
        /// <summary>
        /// label type default WMS 0 bin label 1 carton label 2 pallet label
        /// </summary>
        public int label_type { get; set; } = 0;

        /// <summary>
        /// printer address
        /// </summary>
        public string printer { get; set; }
        /// <summary>
        /// label data
        /// </summary>
        public string data { get; set; }
        /// <summary>
        /// 0 new, 1 printed
        /// </summary>
        public int print_flag { get; set; }
    }

    public abstract class LabelModel { }
    /// <summary>
    /// 储位标签
    /// </summary>
    public class BinLabelModel : LabelModel
    {
        /// <summary>
        /// label type default WMS 0 bin label 1 carton label 2 pallet label
        /// </summary>
        public int label_type { get; set; } = 0;
        /// <summary>
        /// label id
        /// </summary>
        public string label_id { get; set; } = "WMS";
        /// <summary>
        /// printer address
        /// </summary>
        public string material_code { get; set; }
        /// <summary>
        /// 物料类型
        /// </summary>
        public string material_type { get; set; }
        /// <summary>
        /// label data
        /// </summary>
        public string bin_code { get; set; }
        /// <summary>
        /// S: Single, M: Multiple
        /// </summary>
        public string storage_type { get; set; }
        /// <summary>
        /// 库别
        /// </summary>
        public string warehouse_code { get; set; }
        /// <summary>
        /// 供应商代码
        /// </summary>
        public string vendor_code { get; set; }
        /// <summary>
        /// DC FIFO USE
        /// </summary>
        public string date_code { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public decimal qty { get; set; }
        /// <summary>
        /// Keeper
        /// </summary>
        public string keeper_code { get; set; }
        /// <summary>
        /// 操作人
        /// </summary>
        public string user_name { get; set; }
    }
    /// <summary>
    /// 栈板标签
    /// </summary>
    public class PalletLabelModel : LabelModel
    {
        /// <summary>
        /// label type default WMS 0 bin label 1 carton label 2 pallet label
        /// </summary>
        public int label_type { get; set; } = 0;
        /// <summary>
        /// label id
        /// </summary>
        public string label_id { get; set; } = "WMS";
        /// <summary>
        /// material code
        /// </summary>
        public string material_code { get; set; }
        /// <summary>
        /// 机种簇
        /// </summary>
        public string model_family { get; set; }
        /// <summary>
        /// 机种描述
        /// </summary>
        public string model_desc { get; set; }
        /// <summary>
        /// 库别
        /// </summary>
        public string warehouse_code { get; set; }
        /// <summary>
        /// label 储位
        /// </summary>
        public string bin_code { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public decimal qty { get; set; }
        /// <summary>
        /// usn清单
        /// </summary>
        public List<MoUsn> usn_list { get; set; }
    }
    public class MoUsn
    {
        public string mo { set; get; }
        public string usn { set; get; }
    }
    public class TestPageModel : LabelModel
    {
        /// <summary>
        /// Test Message
        /// </summary>
        public string testMsg { get; set; }
    }
    class Print
    {
        
        Configuration config = Program.config;
        public LabelModel model;
        private int page = 0;
        private int pageSize = 3 * 19;
        public Print()
        {
        }
        public void print(string data)
        {
            //process data to print?
            var task = JsonConvert.DeserializeObject<PrintJobModel>(data);
            if (task == null)
            {
                throw new Exception("打印数据不可为空");
            }

            if (task.label_id == null)
            {
                throw new Exception("标签类型不可为空");        
            }
            if (task.data == null)
            {
                throw new Exception("标签信息不可为空");
            }
            //if (task.job_id == null) throw new Exception("任务类型不可为空");            
            switch (task.label_type)
            {
                case 0://BinLabel
                    var bin = JsonConvert.DeserializeObject<BinLabelModel>(task.data);
                    var t = bin.material_type;
                    model = bin;
                    if (t == "ROH") this.print3(); else this.print4();
                    break;
                case 1://CartonLabel
                    //var label = JsonConvert.DeserializeObject<BinLabelModel>(task.data);
                    //this.print1();
                    break;
                case 2://PalletLabel A5
                    model = JsonConvert.DeserializeObject<PalletLabelModel>(task.data);
                    this.print2();
                    break;
                case 3://TestPage
                    model = JsonConvert.DeserializeObject<TestPageModel>(task.data);
                    this.printTest();
                    break;
                case 4://PalletLabel A6
                    model = JsonConvert.DeserializeObject<PalletLabelModel>(task.data);
                    this.print5();
                    break;
                default:
                    throw new Exception("不支持的标签类型");
            }
            Program.writeLog(task.data);
        }

        //Carton Label Print
        /*public void print1()
        {

            var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = config.AppSettings.Settings["printer_name"]?.Value;
            //doc.DocumentName = doc.PrinterSettings.MaximumCopies.ToString();
            doc.PrintPage += new PrintPageEventHandler(this.BinLabelDocument_PrintPage);
            doc.PrintController = new System.Drawing.Printing.StandardPrintController();
            doc.Print();
            doc.Dispose();

        }*/


        /// <summary>
        /// 打印的格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*private void BinLabelDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
         {
            
        var jObject = JObject.Parse(postMsg);
        var str = jObject["data"].ToString();
        var labObject = JObject.Parse(str);
        var array = labObject["usn_list"] as JArray;
        EncodingOptions encodeOption = new EncodingOptions();
        encodeOption.Height = 13;
            encodeOption.Width = 200;
            encodeOption.PureBarcode = true;
            ZXing.BarcodeWriter wr = new ZXing.BarcodeWriter();
        wr.Options = encodeOption;
            wr.Format = ZXing.BarcodeFormat.CODE_128;
            var code = "L2014021700000008";
        Bitmap img = wr.Write(array[""].ToString());

        e.Graphics.DrawString("CartonID: "+labObject["bin_code"].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 0);
            e.Graphics.DrawImage(img, 9, 20);
            e.Graphics.DrawString("Print time: " + DateTime.Now.ToString(), new Font(new FontFamily("Arial"), 10f), Brushes.Black, 10, 35);
            e.Graphics.DrawLine(Pens.Black, 10, 50, 220, 50);
            e.Graphics.DrawString("Material:" + labObject["material_code"].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 55);
            e.Graphics.DrawString("Vendor: " + labObject["vendor_code"].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 67);
            e.Graphics.DrawString("Quantity: " + labObject["qty"].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 79);
            e.Graphics.DrawString("Lot Code: " + labObject["material_code"].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 91);
            e.Graphics.DrawString("Date Code:" + labObject["material_code"].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 103);
            e.Graphics.DrawString("Batch#: " + labObject["material_code"].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 115);
            e.Graphics.DrawString("Tooling#: " + labObject[""].ToString(), new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 127);
            e.Graphics.DrawLine(Pens.Black, 10, 144, 220, 144);
            e.HasMorePages = false;
        }*/
    /// <summary>
    /// 设置PrintDocument 的相关属性
    /// </summary>
    /// <param name="str">要打印的字符串</param>

    //Pallet Label Print Paper A5
    public void print2()
        {
            var doc = new PrintDocument();
            //调用的打印机
            doc.PrinterSettings.PrinterName = config.AppSettings.Settings["printer_name"]?.Value;
            //doc.DocumentName = doc.PrinterSettings.MaximumCopies.ToString();
            doc.PrintPage += new PrintPageEventHandler(this.PalletLabelDocumentA5_PrintPage);
            doc.PrintController = new StandardPrintController();
            doc.Print();
            doc.Dispose();
        }
        /// <summary>
        /// 打印的格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PalletLabelDocumentA5_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {/*
            如果需要改变 可以在new Font(new FontFamily("Arial"), 11）中的“Arial”改成自己要的字体就行了，黑体 后面的数字代表字体的大小
             System.Drawing.Brushes.Blue, 170, 10 中的 System.Drawing.Brushes.Blue 为颜色，后面的为输出的位置*/
            var top = 100;
            var left = 10;
            var pallet = model as PalletLabelModel;
            EncodingOptions encodeOption = new EncodingOptions();
            encodeOption.Height = 10;
            encodeOption.Width = 80;
            encodeOption.PureBarcode = true;
            ZXing.BarcodeWriter wr = new ZXing.BarcodeWriter();
            wr.Options = encodeOption;
            wr.Format = ZXing.BarcodeFormat.CODE_128;
            string printTime = DateTime.Now.ToString();
            Bitmap qtyImg = wr.Write(pallet.qty.ToString());
            Bitmap modelImg = wr.Write(pallet.model_family);
            e.Graphics.DrawString("W/H:" + pallet.warehouse_code, new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 40, 26);
            e.Graphics.DrawString("STORE-IN SHEET", new Font(new FontFamily("Arial"), 18f, FontStyle.Bold), Brushes.Black, 190, 5);
            e.Graphics.DrawString("Print time: " + printTime, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 420, 18);
            //Qty 条形码
            e.Graphics.DrawImage(qtyImg, 265, 76);
            e.Graphics.DrawString("Pallet No: " + pallet.label_id, new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 48);
            e.Graphics.DrawString("Model: " + pallet.model_family, new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 60);
            e.Graphics.DrawImage(modelImg, 260, 48);
            e.Graphics.DrawString("Model Desc: " + pallet.model_desc, new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 200, 60);
            e.Graphics.DrawString("Part No: "+ pallet.material_code, new Font(new FontFamily("Arial"),10f, FontStyle.Bold), Brushes.Black, 10, 72);
            e.Graphics.DrawString("Qty: " + pallet.qty, new Font(new FontFamily("Arial"),10f, FontStyle.Bold), Brushes.Black, 200, 72);
            e.Graphics.DrawString("Bin: " + pallet.bin_code, new Font(new FontFamily("Arial"),10f, FontStyle.Bold), Brushes.Black, 440, 72);
            //双细线画粗线
            e.Graphics.DrawLine(Pens.Black, 195, 30, 405, 30);
            e.Graphics.DrawLine(Pens.Black, 195, 31, 405, 31);
            e.Graphics.DrawLine(Pens.Black, 10, 92, 570,  92 );
           
            var num =0;
            var num2 = 0;
            for (var i = page * pageSize; i < pallet.usn_list.Count; i++) {
                var usn = pallet.usn_list[i];
                var img = wr.Write(usn.usn);
                var row = ((num % 3) * 197);
                num2 = (num > 0 && num % 3 == 0) ? num2 + 1 : num2;
                var col = num2 * 37.0f;
                e.Graphics.DrawString("MO:" + usn.mo, new Font(new FontFamily("Arial"), 7f), Brushes.Black, left + row, top + col);
                e.Graphics.DrawImage(img, 7 + row, 113 + col);
                e.Graphics.DrawString(usn.usn, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 10 + row, 124 + col);
                num++;
                if (num % pageSize == 0) break;
            }

            page++;
            e.HasMorePages = (page * pageSize < pallet.usn_list.Count);
            e.Graphics.DrawString($"Page: {page}", new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 510, 37);

        }


        //ROH Label Print
         public void print3()
        {
            var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = config.AppSettings.Settings["printer_name"]?.Value;
            //doc.DocumentName = doc.PrinterSettings.MaximumCopies.ToString();
            doc.PrintPage += new PrintPageEventHandler(this.ROHLabelDocument_PrintPage);
            doc.PrintController = new System.Drawing.Printing.StandardPrintController();
            doc.Print();
            doc.Dispose();

        }


        /// <summary>
        /// 打印的格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ROHLabelDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {/*
            如果需要改变 可以在new Font(new FontFamily("Arial"), 11）中的“Arial”改成自己要的字体就行了，黑体 后面的数字代表字体的大小
             System.Drawing.Brushes.Blue, 170, 10 中的 System.Drawing.Brushes.Blue 为颜色，后面的为输出的位置*/
            var bin = model as BinLabelModel;
            
            EncodingOptions encodeOption = new EncodingOptions();
            encodeOption.Height = 13;
            encodeOption.Width = 200;
            encodeOption.PureBarcode = true;
            ZXing.BarcodeWriter wr = new ZXing.BarcodeWriter();
            wr.Options = encodeOption;
            wr.Format = ZXing.BarcodeFormat.CODE_128;
            Bitmap img = wr.Write(bin.label_id.ToString());

            e.Graphics.DrawString($"Label ID: " + bin.label_id, new Font(new FontFamily("Arial"), 9f, FontStyle.Bold), Brushes.Black, 23, 0);
            e.Graphics.DrawImage(img, 6, 20);
            e.Graphics.DrawString("Print time: " + DateTime.Now.ToString() + " " + bin.user_name, new Font(new FontFamily("Arial"),6f), Brushes.Black, 25, 37);
            e.Graphics.DrawLine(Pens.Black, 10, 50, 245, 50);
            e.Graphics.DrawString("Material:" +"          "+ bin.material_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 52);
            e.Graphics.DrawString("Bin: " + "                " + bin.bin_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 65);
            e.Graphics.DrawString("Vendor: " + "        " + bin.vendor_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 78);
            e.Graphics.DrawString("GR Date/DC: " +"  "+ bin.date_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 91);
            e.Graphics.DrawString("Quantity:" +"         "+ bin.qty, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 103);
            e.Graphics.DrawString("Version/GP: " , new Font(new FontFamily("Arial"),7f), Brushes.Black, 25, 116);
            e.Graphics.DrawString("UP Point: " , new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 129);
            e.Graphics.DrawString("Keeper/Pos: " + "   " + bin.keeper_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 142);
            e.Graphics.DrawLine(Pens.Black, 10, 159, 245, 159);
            e.Graphics.DrawLine(Pens.Black, 87, 50, 87, 159);
            e.HasMorePages = false;
        }
        // F/G Label Print
        public void print4()
        {
            var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = config.AppSettings.Settings["printer_name"]?.Value;
            //doc.DocumentName = doc.PrinterSettings.MaximumCopies.ToString();
            doc.PrintPage += new PrintPageEventHandler(this.FGLabelDocument_PrintPage);
            doc.PrintController = new System.Drawing.Printing.StandardPrintController();
            doc.Print();
            doc.Dispose();

        }


        /// <summary>
        /// 打印的格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FGLabelDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {/*
            如果需要改变 可以在new Font(new FontFamily("Arial"), 11）中的“黑体”改成自己要的字体就行了，黑体 后面的数字代表字体的大小
             System.Drawing.Brushes.Blue, 170, 10 中的 System.Drawing.Brushes.Blue 为颜色，后面的为输出的位置*/
            var bin = model as BinLabelModel;
            
            EncodingOptions encodeOption = new EncodingOptions();
            encodeOption.Height = 13;
            encodeOption.Width = 200;
            encodeOption.PureBarcode = true;
            ZXing.BarcodeWriter wr = new ZXing.BarcodeWriter();
            wr.Options = encodeOption;
            wr.Format = ZXing.BarcodeFormat.CODE_128;
            Bitmap img = wr.Write(bin.label_id.ToString());
            e.Graphics.DrawString($"Label ID: " + bin.label_id, new Font(new FontFamily("Arial"), 9f, FontStyle.Bold), Brushes.Black, 23, 0);
            e.Graphics.DrawImage(img, 6, 20);
            e.Graphics.DrawString("Print time: " + DateTime.Now.ToString() + " " + bin.user_name, new Font(new FontFamily("Arial"), 6f), Brushes.Black, 25, 37);
            e.Graphics.DrawLine(Pens.Black, 10, 50, 245, 50);
            e.Graphics.DrawString("Material:" + "          " + bin.material_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 52);
            e.Graphics.DrawString("Bin: " + "                " + bin.bin_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 65);
            e.Graphics.DrawString("Vendor: " + "        " + bin.vendor_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 78);
            e.Graphics.DrawString("GR Date/DC: " + "  " + bin.date_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 91);
            e.Graphics.DrawString("Quantity:" + "         " + bin.qty, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 103);
            e.Graphics.DrawString("Version/GP: ", new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 116);
            e.Graphics.DrawString("UP Point: ", new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 129);
            e.Graphics.DrawString("Keeper/Pos: " + "   " + bin.keeper_code, new Font(new FontFamily("Arial"), 7f), Brushes.Black, 25, 142);
            e.Graphics.DrawLine(Pens.Black, 10, 159, 245, 159);
            e.Graphics.DrawLine(Pens.Black, 87, 50, 87, 159);
            e.HasMorePages = false;
        }

        //Pallet Label Print Paper A6
        public void print5()
        {
            var doc = new PrintDocument();
            //调用的打印机
            doc.PrinterSettings.PrinterName = config.AppSettings.Settings["printer_name"]?.Value;
            //doc.DocumentName = doc.PrinterSettings.MaximumCopies.ToString();
            doc.PrintPage += new PrintPageEventHandler(this.PalletLabelDocumentA6_PrintPage);
            doc.PrintController = new StandardPrintController();
            doc.Print();
            doc.Dispose();
        }
        /// <summary>
        /// 打印的格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PalletLabelDocumentA6_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {/*
            如果需要改变 可以在new Font(new FontFamily("Arial"), 11）中的“Arial”改成自己要的字体就行了，黑体 后面的数字代表字体的大小
             System.Drawing.Brushes.Blue, 170, 10 中的 System.Drawing.Brushes.Blue 为颜色，后面的为输出的位置*/
            var top = 67;
            var left = 10;
            var pallet = model as PalletLabelModel;
            EncodingOptions encodeOption = new EncodingOptions();
            encodeOption.Height = 7;
            encodeOption.Width = 100;
            encodeOption.PureBarcode = true;
            ZXing.BarcodeWriter wr = new ZXing.BarcodeWriter();
            wr.Options = encodeOption;
            wr.Format = ZXing.BarcodeFormat.CODE_128;
            string printTime = DateTime.Now.ToString();
            Bitmap qtyImg = wr.Write(pallet.qty.ToString());
            Bitmap modelImg = wr.Write(pallet.model_family);
            e.Graphics.DrawString("W/H:" + pallet.warehouse_code, new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 30, 11);
            e.Graphics.DrawString("STORE-IN SHEET", new Font(new FontFamily("Arial"), 14f, FontStyle.Bold), Brushes.Black, 120, 2);
            e.Graphics.DrawString("Print time: " + printTime, new Font(new FontFamily("Arial"), 5f), Brushes.Black, 300, 12);
            //Qty 条形码
            e.Graphics.DrawImage(qtyImg, 175, 53);
            e.Graphics.DrawString("Pallet No: " + pallet.label_id, new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 10, 28);
            //e.Graphics.DrawString($"Page: {page}", new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 355, 27);
            e.Graphics.DrawString("Model: " + pallet.model_family, new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 10, 39);
            e.Graphics.DrawImage(modelImg, 190, 30);
            e.Graphics.DrawString("Model Desc: " + pallet.model_desc, new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 140, 39);
            e.Graphics.DrawString("Part No: " + pallet.material_code, new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 10, 50);
            e.Graphics.DrawString("Qty: " + pallet.qty, new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 140, 50);
            e.Graphics.DrawString("Bin: " + pallet.bin_code, new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 287, 50);
            //双细线画粗线
            e.Graphics.DrawLine(Pens.Black, 123, 22, 286, 22);
            e.Graphics.DrawLine(Pens.Black, 123, 23, 286, 23);
            e.Graphics.DrawLine(Pens.Black, 10, 65, 400, 65);

            var num = 0;
            var num2 = 0;
            for (var i = page * pageSize; i < pallet.usn_list.Count; i++)
            {
                var usn = pallet.usn_list[i];
                var img = wr.Write(usn.usn);
                var row = ((num % 3) * 133);
                num2 = (num > 0 && num % 3 == 0) ? num2 + 1 : num2;
                var col = num2 * 27.0f;
                e.Graphics.DrawString("MO:" + usn.mo, new Font(new FontFamily("Arial"), 5f), Brushes.Black, left + row, top + col);
                e.Graphics.DrawImage(img, 7 + row, 76 + col);
                e.Graphics.DrawString(usn.usn, new Font(new FontFamily("Arial"), 5f), Brushes.Black, 10 + row, 85 + col);
                num++;
                if (num % pageSize == 0) break;
            }

            page++;
            e.HasMorePages = (page * pageSize < pallet.usn_list.Count);
            e.Graphics.DrawString($"Page: {page}", new Font(new FontFamily("Arial"), 8f, FontStyle.Bold), Brushes.Black, 355, 27);

        }
        public void printTest()
        {
            var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = config.AppSettings.Settings["printer_name"]?.Value;
            //doc.DocumentName = doc.PrinterSettings.MaximumCopies.ToString();
            doc.PrintPage += new PrintPageEventHandler(this.Test_Page);
            doc.PrintController = new System.Drawing.Printing.StandardPrintController();
            doc.Print();
            doc.Dispose();

        }


        /// <summary>
        /// 打印的格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Test_Page(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {/*
            如果需要改变 可以在new Font(new FontFamily("Arial"), 11）中的“黑体”改成自己要的字体就行了，黑体 后面的数字代表字体的大小
             System.Drawing.Brushes.Blue, 170, 10 中的 System.Drawing.Brushes.Blue 为颜色，后面的为输出的位置*/
            var test = model as TestPageModel;
            e.Graphics.DrawString($"测试信息: " + test.testMsg, new Font(new FontFamily("Arial"), 10f, FontStyle.Bold), Brushes.Black, 10, 0);
            e.HasMorePages = false;
        }

    }
}
