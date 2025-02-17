﻿using AAXClean;
using System;
using System.Text;
using System.Diagnostics;
using static Other;
using System.IO;
using NAudio.Wave;
using NAudio.Lame;
using NAudio;
using NAudio.MediaFoundation;
using System.Threading;
using Aax.Activation.ApiClient;

namespace Files
{
    internal class Create
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;
        public static string nfo(string aax, string file, bool split = false)
        {
            string[] nfoPart = new string[15];
            try
            {
                aax = string.Concat("\"", aax, "\"");
                file = string.Concat("\"", file, "\"");
                

                string mi = $"{root}src\\tools\\mediainfo.exe";
                nfoPart[0] = SoftWare(mi, $"{aax} --Inform=General;%Album%", false);            //Title
                nfoPart[1] = SoftWare(mi, $"{aax} --Inform=General;%Performer%", false);        //Author
                nfoPart[2] = SoftWare(mi, $"{aax} --Inform=General;%nrt%", false);              //Narrator
                nfoPart[3] = SoftWare(mi, $"{aax} --Inform=General;%Copyright%", false);        //Copyright
                nfoPart[4] = SoftWare(mi, $"{aax} --Inform=General;%Genre%", false);            //Genre
                nfoPart[5] = SoftWare(mi, $"{aax} --Inform=General;%pub%", false);              //Publisher
                nfoPart[6] = SoftWare(mi, $"{aax} --Inform=General;%rldt%", false);             //Release Date
                nfoPart[7] = SoftWare(mi, $"{aax} --Inform=General;%Duration/String2%", false); //Duration (h, m)
                nfoPart[8] = SoftWare(mi, $"{aax} --Inform=\"Menu;%FrameCount%\"", false);      //Chapters
                nfoPart[9] = SoftWare(mi, $"{aax} --Inform=General;%Format%", false);           //general format
                nfoPart[10] = SoftWare(mi, $"{aax} --Inform=Audio;%Format%", false);            //audio format
                nfoPart[11] = SoftWare(mi, $"{aax} --Inform=Audio;%BitRate%", false);           //source bitrate
                try
                {
                    nfoPart[11] = (Int32.Parse(nfoPart[11]) / 1024).ToString();
                }
                catch
                {
                    nfoPart[11] = "NULL";
                    Alert.Error("Failed Getting Source BitRate");
                }
                if ((Path.GetExtension(file.Replace("\"", "")) == ".m4b"))
                {
                    nfoPart[12] = SoftWare(mi, $"{file} --Inform=General;%CodecID%", false); //encoded codecID
                }
                else
                {
                    string mp3enc = string.Empty;
                    switch (split)
                    {
                        case true: mp3enc = "LAME 3.100";
                            break;
                        case false: mp3enc = "Lavf59.16.100";
                            break;
                    }
                    nfoPart[12] = $"{mp3enc} MP3";
                }
                nfoPart[13] = (TagLib.File.Create(file.Replace("\"", ""))).Properties.AudioBitrate.ToString();
                nfoPart[14] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false); //comment (Track_More)
            }
            catch (Exception ex)
            {
                Alert.Error($"NFO Failed: {ex.Message}");
            }

            string nfo = @$"General Information
===================
 Title:                  {nfoPart[0]}
 Author:                 {nfoPart[1]}
 Narrator:               {nfoPart[2]}
 AudioBook Copyright:    {nfoPart[3].Replace("&#169;", string.Empty).Replace(";", " ")}
 Genre:                  {nfoPart[4]}
 Publisher:              {nfoPart[5]}
 Published:              {nfoPart[6]}
 Duration:               {nfoPart[7]}
 Chapters:               {nfoPart[8]}

Media Information
=================
 Source Format:          Audible {nfoPart[9].ToUpper()} ({nfoPart[10]})
 Source Bitrate:         {nfoPart[11]} kbps

 Lossless Encode:        {(Path.GetExtension(file.Replace("\"", "")) == ".m4b")}
 Encoded Codec:          {nfoPart[12]}
 Encoded Bitrate:        {nfoPart[13]} kbps

 Ripper:                 {Workings.iAmDeaf.mark} {Workings.iAmDeaf.version}

Publisher's Summary
===================
{nfoPart[14]}
";
            return nfo;
        }

        public static void Cue(string aax, string file, string codec, string format)
        {
            var ch = SoftWare($"{AppDomain.CurrentDomain.BaseDirectory}src\\tools\\mediainfo.exe", $"\"{aax}\"", false);
            //split the chapters
            ch = ch.Substring(ch.IndexOf("Frame count"));

            string PID = Process.GetCurrentProcess().Id.ToString();
            string _temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"src\\data\\dump\\{PID}.txt");
            File.WriteAllText(_temp, ch);

            string[] chpts = File.ReadAllLines(_temp);
            string[] chtm = new string[chpts.Length];
            string[] chndx = new string[chpts.Length];
            string[] chnm = new string[chpts.Length];
            string chapterFile = string.Empty;

            for (int i = 2; i < chpts.Length; i++)
            {
                //Chapter timings
                chtm[i] = chpts[i].Substring(0, chpts[i].LastIndexOf(":")).Trim();
                //Chapter names
                chnm[i] = chpts[i].Substring(chpts[i].LastIndexOf(":") + 1).Trim();
                //Chapter index
                chndx[i] = chnm[i].Replace("Chapter", string.Empty).Trim();
                if (chndx[i].Length < 2)
                {
                    chndx[i] = string.Concat("0", chndx[i]);
                }

                chapterFile += $"CHAPTER{chndx[i]}={chtm[i]}\nCHAPTER{chndx[i]}NAME={chnm[i]}"+Environment.NewLine;
            }

            File.WriteAllText(_temp, chapterFile.Trim());
            string cueArg = $@"{root}src\tools\cuegen.vbs {root}src\\data\\dump\\{PID}.txt";
            var cueGen = Process.Start(@"cmd", @"/c " + cueArg);

            cueGen.WaitForExit();
            cueGen.Close();
            cueGen.Dispose();

            string[] cue = File.ReadAllLines($"{root}src\\data\\dump\\{PID}.cue");
            cue[0] = $"FILE \"{Path.GetFileName($"{file}.{codec}")}\" {format.ToUpper()}";
            File.WriteAllLines($"{file}.cue", cue);

            if (!File.Exists($"{file}.cue"))
            {
                Alert.Error("Cue Failed");
            }
            else
            {
                Alert.Success("Cue Created");
            }

            File.Delete($@"{root}src\data\dump\{PID}.cue");
            File.Delete($@"{root}src\data\dump\{PID}.txt");
        }


        public static void AudioBook(string bytes, string aax, string file, string ext = "m4b", bool split = false)
        {


            if (!(ext == "m4b" || ext == "mp3"))
            {
                Alert.Notify($"Invalid codec {ext}. Defaulting to M4B");
                ext = "m4b";
            }

            var aaxFile = new AAXClean.AaxFile(File.OpenRead(aax));
            aaxFile.SetDecryptionKey(bytes);

            if (ext == "m4b")
            {
                if (!split)
                {
                    try
                    {
                        aaxFile.ConvertToMp4a(File.Open($"{file}.m4b", FileMode.OpenOrCreate, FileAccess.ReadWrite));
                        aaxFile.Close();

                        if (File.Exists($"{file}.m4b"))
                        {
                            Alert.Success("AudioBook Created");
                        }
                        else
                        {
                            Alert.Error("AudioBook Creation Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        Alert.Error(ex.Message);
                    }
                }
                else
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"src\data\dump\{Process.GetCurrentProcess().Id}"), Path.GetDirectoryName(file));

                        aaxFile.ConvertToMultiMp4a(aaxFile.GetChapterInfo(), NewSplit);

                        static void NewSplit(NewSplitCallback newSplitCallback)
                        {
                            string dir = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"src\data\dump\{Process.GetCurrentProcess().Id}"));

                            string fileName = newSplitCallback.Chapter.Title.Replace(":", "") + ".m4b";

                            newSplitCallback.OutputFile = File.OpenWrite(Path.Combine(dir, fileName));
                        }
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"src\data\dump\{Process.GetCurrentProcess().Id}"));
                    }
                    catch (Exception ex)
                    {
                        Alert.Error(ex.Message);
                    }
                }
            }

            if (ext == "mp3" && split == false)
            {
                string PID = Process.GetCurrentProcess().Id.ToString();
                //Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                try
                {
                    string _temp = Path.Combine(root, "src\\data\\dump", $"{PID}.mp4");
                    aaxFile.ConvertToMp4a(File.Open($"{_temp}", FileMode.OpenOrCreate, FileAccess.ReadWrite));
                    aaxFile.Close();

                    Load.LoadLameDLL();

                    TagLib.File mp4 = TagLib.File.Create(_temp);
                    int br = Int32.Parse(string.Concat(mp4.Properties.AudioBitrate.ToString(), "000"));

                    string nrt = SoftWare($@"{root}\\src\tools\mediainfo.exe", $"\"{_temp}\" --Inform=General;%nrt%", false);
                    string comment = SoftWare($@"{root}\\src\tools\mediainfo.exe", $"\"{_temp}\" --Inform=General;%Track_More%", false);

                    Alert.Notify($"Lavf59.16.100 - {br.ToString()[..3]}_CBR");
                    
                    MediaFoundationApi.Startup();
                    var aacFilePath = $@"{root}\\src\data\dump\{PID}.mp3";
                    using (var reader = new MediaFoundationReader(_temp))
                    {
                        MediaFoundationEncoder.EncodeToMp3(reader, aacFilePath, br);
                    }

                    Alert.Notify("Tagging File");

                    SoftWare($@"{root}\src\tools\ffmpeg.exe", $"-i \"{_temp}\" -i  \"{root}\\src\\data\\dump\\{PID}.mp3\" -map 1 -metadata Narrator=\"{nrt}\" -metadata Comment=\"{comment.Replace("\"", string.Empty)}\" -c copy \"{file}.mp3\" -y", true);
                    SoftWare($@"{root}\src\tools\ffmpeg.exe", $"-i \"{_temp}\" -map 0:v -map -0:V -c copy \"{root}\\src\\data\\dump\\{PID}.jpg\" -y", true);
                }
                catch (Exception ex)
                {
                    Alert.Error(ex.Message);
                }

                if (!(Embed.SetCoverArt(string.Concat(file, ".mp3"), $"{root}src\\data\\dump\\{PID}.jpg")))
                {
                    Alert.Notify("Unable to set cover art");
                }


                File.Delete($@"{root}\\src\\data\\dump\\{PID}.mp4");
                File.Delete($@"{root}\\src\\data\\dump\\{PID}.mp3");
                File.Delete($@"{root}\\src\\data\\dump\\{PID}.jpg");
            }
            else
            {
                if (split && ext == "mp3")
                {
                    Load.LoadLameDLL();
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"{root}\\src\data\dump\{Process.GetCurrentProcess().Id}"), Path.GetDirectoryName(file));

                    var chapters = aaxFile.GetChapterInfo();

                    LameConfig lameConfig = new LameConfig();
                    lameConfig.Preset = Get.Preset(aax);


                    aaxFile.ConvertToMultiMp3(aaxFile.GetChapterInfo(), NewSplit, lameConfig);

                    static void NewSplit(NewSplitCallback newSplitCallback)
                    {
                        string dir = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"src\data\dump\{Process.GetCurrentProcess().Id}"));

                        string fileName = newSplitCallback.Chapter.Title.Replace(":", "") + ".mp3";

                        newSplitCallback.OutputFile = File.OpenWrite(Path.Combine(dir, fileName));
                    }
                    File.Delete($@"{root}\\src\data\dump\{Process.GetCurrentProcess().Id}");
                }
            }
        }
    }

    public class Embed
    {
        public static bool SetCoverArt(string AudioFile, string CoverFile)
        {
            if(!(File.Exists(AudioFile) || File.Exists(CoverFile)))
            {
                Alert.Error("Audio or Cover missing!");
                return false;
            }

            try
            {
                // Формируем картинку в кадр Id3v2
                TagLib.Id3v2.Tag.DefaultVersion = 3;
                TagLib.Id3v2.Tag.ForceDefaultVersion = true;

                TagLib.File TagLibFile = TagLib.File.Create(AudioFile);
                TagLib.Picture picture = new TagLib.Picture(CoverFile);
                TagLib.Id3v2.AttachmentFrame albumCoverPictFrame = new TagLib.Id3v2.AttachmentFrame(picture);
                albumCoverPictFrame.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                albumCoverPictFrame.Type = TagLib.PictureType.FrontCover;
                TagLib.IPicture[] pictFrames = new TagLib.IPicture[1];
                pictFrames[0] = (TagLib.IPicture)albumCoverPictFrame;
                TagLibFile.Tag.Pictures = pictFrames;
                TagLibFile.Save();

                return true;
            }
            catch (Exception ex)
            {
                Alert.Error($"Unable to set cover art: {ex.Message}");
                return false;
            }
        }
    }

    public class Get
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;

        internal static async Task<string> aaxcAsync(string checksum)
        {
            return AaxActivationClient.Instance.ResolveActivationBytes(checksum).Result.ToString();
        }
        internal static string Hash(string file)
        {
            try
            {
                using (var fs = System.IO.File.OpenRead(file))
                using (var br = new BinaryReader(fs))
                {
                    fs.Position = 0x251 + 56 + 4;
                    var checksum = br.ReadBytes(20);
                    return bytes(checksum);
                }
            }
            catch(Exception ex)
            {
                Alert.Error("Error calculating Hash: "+ ex.Message);
            }
            return String.Empty;
        }

        internal static string bytes(byte[] bt)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bt)
                    sb.Append(b.ToString("X2"));

                string hexString = sb.ToString();
                return hexString;
            }
            catch (Exception ex)
            {
                Alert.Error("Converting Hash to Hex: "+ex.Message);
                return String.Empty;
            }
        }

        
        public static string ActivationBytes(string aax)
        {
            var checksum = Hash(aax);
            string[] keys = File.ReadAllLines(Path.Combine(root, @"src\data\KeyHistory\log"));

            for (int i = 0; i < keys.Length; i+=2)
            {
                if (keys[i] == checksum)
                {
                    return keys[i+1];
                }
            }

            string bytes = string.Empty;
            try
            {
                bytes = AaxActivationClient.Instance.ResolveActivationBytes(checksum).Result.ToString();
            }
            catch(Exception ex)
            {
                Alert.Error("Key not found in offline log: "+ex.Message);
                return string.Empty;
            }
            File.AppendAllText(Path.Combine(root, @"src\data\KeyHistory\log"), $"{checksum}\n{bytes}" + Environment.NewLine);
            return bytes;
        }
        
        public static string[] AaxInformation(string aax)
        {
            aax = String.Concat("\"", aax, "\"");
            string mi = $"{root}src\\tools\\mediainfo.exe";
            string[] info = new string[5];
            info[0] = SoftWare(mi, $"{aax} --Inform=General;%Album%", false);
            info[1] = info[0].Split(",").Last().Trim();
            info[2] = info[0].Replace(info[1], null).Replace(",", null).Split(":").Last().Trim();
            info[3] = info[0].Replace(info[1], null).Replace(info[2], null).Replace(":", null).Replace(",", null).Trim();
            info[4] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false);
            info[1] = info[1].Replace(" ", null).Replace("Book", null);

            if (info[1].Contains("Volume"))
            {
                info[1] = info[1].Replace("Volume", "Volume ");
            }

            if (info[1].Length < 2)
            {
                info[1] = String.Concat("0", info[1]);
            }

            return info;
        }

        public static NAudio.Lame.LAMEPreset Preset (string file)
        {
            int bitrate = (Int32.Parse(SoftWare(@"src\tools\mediainfo.exe", $"\"{file}\" --Inform=Audio;%BitRate%", false)) / 1024);
            
            switch (bitrate)
            {
                case >= 124:
                    Alert.Notify($"LAME 3.100 - V1"); return LAMEPreset.V1;
                case >= 95:
                    Alert.Notify($"LAME 3.100 - ABR_96"); return LAMEPreset.ABR_96;
                case >= 60:
                    Alert.Notify($"LAME 3.100 - ABR_64"); return LAMEPreset.ABR_64;
                case >= 31:
                    Alert.Notify($"LAME 3.100 - ABR_32"); return LAMEPreset.ABR_32;
                default:
                    Alert.Notify($"LAME 3.100 - ABR_32"); return LAMEPreset.ABR_32;
            }
        }

        public static void Monitor(string file)
        {
            decimal buffer;
            while (!File.Exists(file))
            {
                Thread.Sleep(700);
            }
            while (true)
            {
                buffer = (decimal)new System.IO.FileInfo(file).Length;
                Console.Write($"  {Decimal.Round(buffer / 1048576, 2)} MB");
                Console.Write("\r");
                Thread.Sleep(78);
                if (buffer == (new System.IO.FileInfo(file).Length))
                {
                    Alert.Success($"File size of {Decimal.Round(buffer / 1048576, 2)} MB");
                    break;
                }
            }
        }
    }

    class Load
    {
        public static void LoadLameDLL()
        {
            LameDLL.LoadNativeDLL(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\data"));
        }
    }
}