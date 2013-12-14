using System;
using System.IO;
using System.Text;
using Microsoft.SPOT;
using System.Threading;

namespace CellularRemoteControl
{
    class FileTools
    {
        public FileTools()
        {
        }

        public static string Replace(string Content, string ToReplace, string ReplaceWith)
        {
            string NewString = "";

            if ((ToReplace.Length == 1) && (ReplaceWith.Length > 1))
            {
                string[] tmpContent = Content.Split(ToReplace.ToCharArray());
                for (int i = 0; i < tmpContent.Length; i++)
                {
                    NewString += tmpContent[i] + ReplaceWith;
                }
                return NewString;
            }
            else
            {
                StringBuilder tmpContent = new StringBuilder(Content);
                return tmpContent.Replace(ToReplace, ReplaceWith).ToString();
            }


        }

        public static string ReadString(string Filename)
        {
            string FileContent = "";
            try
            {
                // READ FILE
                FileStream filestream = new FileStream(@"SD\\" + Filename, FileMode.Open);
                StreamReader reader = new StreamReader(filestream);
                FileContent = reader.ReadToEnd();
                reader.Close();
                StringBuilder RemoveCRLF = new StringBuilder(FileContent);
                FileContent = RemoveCRLF.Replace("\r\n", "").ToString();
                return FileContent;
            }
            catch (Exception ex)
            {
                Program._led_Active.Write(true);
                Program._led_NewMessage.Write(true);
                Thread.Sleep(10000);
                Debug.Print(ex.Message);
                return "";
            }
        }

        public static Boolean New(string Filename, string PathFile, string ContentFile)
        {
            try
            {
                string CreateNewFile = @"\SD\" + PathFile + "\\" + Filename;
                FileStream filestream = new FileStream(CreateNewFile, FileMode.Create, FileAccess.Write, FileShare.None);

                if (ContentFile.Length > 0)
                {
                    StreamWriter streamWriter = new StreamWriter(filestream);
                    streamWriter.WriteLine(ContentFile);
                    streamWriter.Close();
                }
                filestream.Close();
                Debug.Print(Filename + ": created");
                return true;
            }
            catch
            {

                Debug.Print(Filename + ": error");
                return false;
            }
        }

        public static Boolean Add(string Filename, string PathFile, string ContentFile)
        {
            try
            {
                string CreateNewFile = @"\SD\" + PathFile + "\\" + Filename;
                FileStream filestream = new FileStream(CreateNewFile, FileMode.Append, FileAccess.Write, FileShare.None);

                if (ContentFile.Length > 0)
                {
                    StreamWriter streamWriter = new StreamWriter(filestream);
                    streamWriter.WriteLine(ContentFile);
                    streamWriter.Close();
                }
                filestream.Close();
                Debug.Print(Filename + ": created");
                return true;
            }
            catch
            {

                Debug.Print(Filename + ": error");
                return false;
            }
        }

        public static string strMID(string String,int Start, int End)
        {
            string NewString = "";
            try
            {
                char[] chr = String.ToCharArray();
                for (int t = Start; t < End; t++)
                {
                    NewString += chr[t];
                }
                return NewString;
            }
            catch
            {
                return "";
            }
        }
    }

}
