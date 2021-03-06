using System;
using System.Data;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using MakingWaves.TravelExp.Impl.TravelExpense.Processing;
using MakingWaves.Common.WS.Exceptions;
using MakingWaves.Common.WS.Utils;
using System.Text;
using System.IO;

/// <summary>
/// Webservice used for manipulating user's data entered to the Flex UI.
/// </summary>
[WebService(Namespace = "http://makingwaves.no/travelExp/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[ToolboxItem(false)]
public class TravelExpense : System.Web.Services.WebService
{
    //log4net
    private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(TravelExpense));

    [WebMethod(Description = "Test if webservice is working, and get current version of the assembly")]
    public string GetVersion()
    {
        log.Debug("GetVersion");
        MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService service =
            new MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService();
        String res = service.GetVersion();
        log.Debug("Returning " + res);
        return res;
    }

    /// <summary>
    /// Gets the travel PDF.
    /// </summary>
    /// <param name="travel">The travel.</param>
    /// <returns></returns>
    [WebMethod(Description = "Generates PDF file from specified data structures. Returns array of bytes (wrapped in other structure).")]
    public MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelReportDocumentVO
        getTravelPdf(MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO travel)
    {
        try
        {
            log.Debug("================================================================");
            log.Debug("=====================  BEGIN - getTravelPdf  ===================");
            log.Debug("getTravelPdf, travel=" + travel.ToString());
            MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService service =
                new MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService();
            MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelReportDocumentVO res =
                service.getTravelPdf(travel);
            log.Debug("=======================  END - getTravelPdf  ===================");
            log.Debug("================================================================");
            return res;
        }
        catch (Exception exc)
        {
            MWBaseException.DefaultExceptionHandler(exc);
            throw exc;
        }
    }

    /// <summary>
    /// Gets the travel PDF as stored id.
    /// </summary>
    /// <param name="travel">The travel.</param>
    /// <returns>Unique ID that can be used later on page</returns>
    /// 
    [WebMethod(Description = "Stores specified 'Travel' strucure at server-side and returns its unique identifier. Use GetStoredData.aspx?id=... page to retrieve PDF file.")]
    public string getTravelPdfAsStoredId(MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO travel)
    {
        try
        {
            log.Debug("================================================================");
            log.Debug("=================  BEGIN - getTravelPdfAsStoredId  =============");
            log.Debug("getTravelPdfAsStoredId, travel=" + travel.ToString());
            MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService service =
                new MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService();
            MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelReportDocumentVO resPdf = service.getTravelPdf(travel);
            string pdfId = StoredDataRepository.Instance.AddNewEntry(new StoredDataEntry("application/pdf", resPdf.PdfFileBytes));            
            log.Debug("getTravelPdfAsStoredId: returning " + pdfId);
            log.Debug("===================  END - getTravelPdfAsStoredId  =============");
            log.Debug("================================================================");
            return pdfId;
        }
        catch (Exception exc)
        {
            MWBaseException.DefaultExceptionHandler(exc);
            throw exc;
        }

    }

    [WebMethod(Description = "Stores specified 'Travel' structure as serialized Xml and returns unique identifier. Use GetStoredData.aspx?id=... page to retrieve XML file.")] 
    public string getTravelXmlAsStoredId(MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO travel)
    {
        try
        {
            log.Debug("================================================================");
            log.Debug("=================  BEGIN - getTravelXmlAsStoredId  =============");
            MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService service =
                new MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService();
            byte[] xmlBin = service.getTravelXml(travel);
            StoredDataEntry entry = new StoredDataEntry("application/download", xmlBin);        
            entry.ForceDownload = true;
            entry.SaveAsFileName = GenerateFileName(travel);
            string id = StoredDataRepository.Instance.AddNewEntry(entry);
            log.Debug("===================  END - getTravelXmlAsStoredId  =============");
            log.Debug("================================================================");
            return id;
        }
        catch (Exception exc)
        {
            MWBaseException.DefaultExceptionHandler(exc);
            throw exc;
        }

    }

    /// <summary>
    /// Generates the name of the file that user can save to the disk.
    /// This file name is suggested by the browser.
    /// The file name is of the format Reiseregning-lastname_firstname_travelstartdate.xml.
    /// </summary>
    /// <param name="travel">The travel.</param>
    /// <returns></returns>
    private string GenerateFileName(MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO travel)
    {
        // Added by KM 2009-03-17

        if (travel == null)
            return "TravelData.xml";
        StringBuilder sb = new StringBuilder();
        sb.Append("Reiseregning");
        if (travel.personalinfo == null)
            return sb.ToString()+".xml";
        sb.Append("-");
        if (travel.personalinfo.lastname!=null)
        {
            sb.Append(NormalizeToFileName(travel.personalinfo.lastname, 15));
            sb.Append("_");
        }
        if (travel.personalinfo.firstname != null)
        {
            sb.Append(NormalizeToFileName(travel.personalinfo.firstname, 15));
            sb.Append("_");
        }
        if (travel.travel.travel_date_out.HasValue)
        {
            sb.Append(travel.travel.travel_date_out.Value.ToString("yyyy'-'MM'-'dd"));
        }
        sb.Append(".xml");
        return sb.ToString();
    }

    /// <summary>
    /// Normalizes the name of to file.
    /// Replaces all occurences of spaces to underscores. Accepts all alphanumeric chars and digits.
    /// Replaces norwegian chars with their similar english characters.
    /// </summary>
    /// <param name="p">The p.</param>
    /// <param name="p_2">The P_2.</param>
    /// <returns></returns>
    private String NormalizeToFileName(string txt, int maxLength)
    {
        // Added by KM 2009-03-17

        String acceptableChars = "-_";
        String replaceChars1 = "ÅåØøÆæ ";
        String replaceChars2 = "AaOoAa_";
        StringBuilder res = new StringBuilder();
        for (int i = 0; i < txt.Length; ++i)
        {
            char ch = txt[i];
            if ((IsEnglishLetter(ch)) || (Char.IsDigit(ch)) || (acceptableChars.IndexOf(ch) != -1))
                res.Append(ch);
            else
            {
                int replIdx = replaceChars1.IndexOf(ch);
                if (replIdx != -1)
                    res.Append(replaceChars2[replIdx]);
            }
            // in other cases, ignore this character.
        }
        String resStr = res.ToString();
        if (resStr.Length > maxLength)
            resStr = resStr.Substring(0, maxLength);
        return resStr;
            
    }

    /// <summary>
    /// Determines whether the specified char is a 'normal' 8-bit ASCII letter.
    /// </summary>
    /// <param name="ch">The ch.</param>
    /// <returns>
    /// 	<c>true</c> if [is english letter] [the specified ch]; otherwise, <c>false</c>.
    /// </returns>
    private bool IsEnglishLetter(char ch)
    {
        return ((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z'));
    }

    /// <summary>
    /// Removes the travel PDF stored id.
    /// </summary>
    /// <param name="id">The id got from method getTravelPdfAsStoredId.</param>
    /// <returns>true if specified id was found and deleted, false if not found</returns>
    [WebMethod(Description = "Clean server-side memory from old, no-more required data. Specify identifier returned by 'getTravelPdfAsStoredId' or other Data-Store method")]
    public bool removeDataStoredId(string id)
    {
        try
        {
            log.Debug("=================== removeTravelPdfStoredId =====================");
            
            //log.Debug("Temporarily disabled for debug puroposes");

            bool res = StoredDataRepository.Instance.DeleteEntry(id);
            log.Info("Deleted PdfRepository item with id="+id);
            return res;
        }
        catch (Exception exc)
        {
            MWBaseException.DefaultExceptionHandler(exc);
            throw exc;
        }
    }

    [WebMethod(Description="Load XML file data and intepret it as TravelExpenseVO serialized object")]
    public MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO
        getTravelObjectFromXml(byte[] xmlFileBytes)
    {
        try
        {
            log.Debug("================================================================");
            log.Debug("=================  BEGIN - GetTravelObjectFromXml  =============");
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO));
            FixXmlIfNecessary(ref xmlFileBytes);
            System.IO.MemoryStream stream = new System.IO.MemoryStream(xmlFileBytes);
            log.Debug("GetTravelObjectFromXml: xmlFileBytes.Length = " + xmlFileBytes.Length);
            object resObj = ser.Deserialize(stream);
            MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO res = resObj as MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO;
            log.Debug("===================  END - GetTravelObjectFromXml  =============");
            log.Debug("================================================================");
            return res;
        }
        catch (Exception exc)
        {
            MWBaseException.DefaultExceptionHandler(exc);
            if ((exc is InvalidOperationException) || (exc is System.Xml.XmlException))
                log.Error("Invalid Xml content is: " + ByteArr2String(xmlFileBytes));
            throw exc;
        }
    }

    /// <summary>
    /// Fixes the XML if necessary.
    /// Sometimes there is an unspecified error during saving of XML file, and this error message
    /// is appended to the XML file. This should be removed, because XML cannot contain multiple root tags.
    /// </summary>
    /// <param name="xmlFileBytes">The XML file bytes.</param>
    private void FixXmlIfNecessary(ref byte[] xmlFileBytes)
    {
        // manually find phrase '</TravelExpenseVO><html>'
        String patternStr = "</TravelExpenseVO><html>";
        String patternPartToLeaveStr = "</TravelExpenseVO>";
        byte[] patternBytes = new byte[patternStr.Length];
        // create patternBytes content
        for(int i=0;i<patternStr.Length;++i)
            patternBytes[i] = (byte)patternStr[i];
        // search
        for (int cXml=0;cXml<xmlFileBytes.Length;++cXml)
        {
            bool matches = true;
            // found begginning, check if whole matches
            for (int cPattern=0;cPattern<patternBytes.Length;++cPattern)
            {
                if (cXml+cPattern>=xmlFileBytes.Length)
                    break;
                if (xmlFileBytes[cXml+cPattern] != patternBytes[cPattern])
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
            {
                byte[] resArr = CopyPartOfArr(xmlFileBytes, 0, cXml + patternPartToLeaveStr.Length);
                byte[] removedArr = CopyPartOfArr(xmlFileBytes, cXml + patternPartToLeaveStr.Length, xmlFileBytes.Length - (cXml + patternPartToLeaveStr.Length));
                xmlFileBytes = resArr;
                String errorInfoStr = ByteArr2String(removedArr);
                log.Error("!!!!! Removed error information from input XML:" + errorInfoStr);
                return;
            }
        }
        // return not changed
    }

    private string ByteArr2String(byte[] byteArr)
    {
        if (byteArr == null)
            return "(null)";
        StringBuilder res = new StringBuilder();
        for(int i=0;i<byteArr.Length;++i)
            res.Append((char)byteArr[i]);
        return res.ToString();
    }

    private byte[] CopyPartOfArr(byte[] sourceArr, int startIdx, int length)
    {
        if ((sourceArr == null) || (startIdx < 0) || (length < 0))
            throw new ArgumentException();
        byte[] resArr = new byte[length];
        for (int i = 0; i < length; ++i)
            resArr[i] = sourceArr[startIdx + i];
        return resArr;
    }

    [WebMethod(Description = "Load XML file data and intepret it as TravelExpenseVO serialized object")]
    public MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO
        getTravelObjectFromXmlAsStoredId(string id)
    {
        byte[] xmlFileBytes = null;
        try
        {
            log.Debug("================================================================");
            log.Debug("=================  BEGIN - GetTravelObjectFromXmlAsStroedId  =============");
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO));
            xmlFileBytes = StoredDataRepository.Instance.GetEntry(id).Data;
            FixXmlIfNecessary(ref xmlFileBytes);
            System.IO.MemoryStream stream = new System.IO.MemoryStream(xmlFileBytes);
            log.Debug("GetTravelObjectFromXmlAsStroedId: xmlFileBytes.Length = " + xmlFileBytes.Length);
            object resObj = ser.Deserialize(stream);
            MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO res = resObj as MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO;
            log.Debug("===================  END - GetTravelObjectFromXmlAsStroedId  =============");
            log.Debug("================================================================");
            return res;
        }
        catch (Exception exc)
        {
            MWBaseException.DefaultExceptionHandler(exc);
            if ((exc is InvalidOperationException) || (exc is System.Xml.XmlException))
                log.Error("Invalid Xml content is: " + ByteArr2String(xmlFileBytes));
            throw exc;
        }
    }

    [WebMethod(Description = "Test method that returns sample pdf")]
    public string RunTestAndGetId()
    {
        try
        {
            MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO travel =
                GetTravelObjectFromXml_Test();
            log.Debug("================================================================");
            log.Debug("=================  BEGIN - RunTestAndGetId  =============");
            MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService service =
                new MakingWaves.TravelExp.Impl.TravelExpense.TravelExpenseService();
            MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelReportDocumentVO resPdf =
                service.getTravelPdf(travel);
            log.Debug("getTravelPdfAsStoredId, travel=" + travel.ToString());
            string pdfId = StoredDataRepository.Instance.AddNewEntry(new StoredDataEntry("application/pdf", resPdf.PdfFileBytes));
            log.Debug("getTravelPdfAsStoredId: returning " + pdfId);
            log.Debug("===================  END - RunTestAndGetId  =============");
            log.Debug("================================================================");
            return pdfId;
        }
        catch (Exception exc)
        {
            MWBaseException.DefaultExceptionHandler(exc);
            throw exc;
        }
    }

    private MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO GetTravelObjectFromXml_Test()
    {
        String travelDataSource = System.Configuration.ConfigurationManager.AppSettings[
            "TravelExpense.Test.GetTravelObjectFromXml_Test.TravelDataXmlFile"];
        log.Info("Running GetTravelObjectFromXml_Test");
        log.Debug("TravelData.xml file is in " + travelDataSource);
        byte[] travelXmlBytes = File.ReadAllBytes(travelDataSource);
        MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO travel =
            getTravelObjectFromXml(travelXmlBytes);
            //DebugEx.LoadObjectContent(travelDataSource,
            //typeof(MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO)) as
            //MakingWaves.TravelExp.Impl.TravelExpense.DataStructures.TravelExpenseVO;
        return travel;
    }
}
