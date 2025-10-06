using System.Runtime.InteropServices;
using System.Text;

namespace GaiaPrintAPI.Helpers
{
    public static class RawPrinterHelper
    {
        // ESTRUCTURAS
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName = string.Empty;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pOutputFile = string.Empty;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType = string.Empty;
        }

        // IMPORTACIONES Winspool
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPWStr)] string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int Level, [In] DOCINFO pDocInfo);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        /// <summary>
        /// Envía bytes directamente a la impresora
        /// </summary>
        public static bool SendBytesToPrinter(string szPrinterName, byte[] pBytes)
        {
            if (string.IsNullOrWhiteSpace(szPrinterName))
                throw new ArgumentNullException(nameof(szPrinterName));
            if (pBytes == null)
                throw new ArgumentNullException(nameof(pBytes));

            IntPtr hPrinter = IntPtr.Zero;
            IntPtr unmanagedBytes = IntPtr.Zero;
            int dwWritten = 0;
            bool success = false;

            DOCINFO docInfo = new DOCINFO()
            {
                pDocName = "Raw Print Job",
                pDataType = "RAW"
            };

            try
            {
                // Abrir impresora
                if (!OpenPrinter(szPrinterName, out hPrinter, IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    Console.WriteLine($"OpenPrinter fallo. Printer='{szPrinterName}' Error={err}");
                    return false;
                }

                // Iniciar documento
                if (!StartDocPrinter(hPrinter, 1, docInfo))
                {
                    int err = Marshal.GetLastWin32Error();
                    Console.WriteLine($"StartDocPrinter fallo. Error={err}");
                    ClosePrinter(hPrinter);
                    return false;
                }

                // Iniciar página
                if (!StartPagePrinter(hPrinter))
                {
                    int err = Marshal.GetLastWin32Error();
                    Console.WriteLine($"StartPagePrinter fallo. Error={err}");
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                    return false;
                }

                // Asignar memoria no administrada
                unmanagedBytes = Marshal.AllocHGlobal(pBytes.Length);
                Marshal.Copy(pBytes, 0, unmanagedBytes, pBytes.Length);

                // Escribir datos
                if (!WritePrinter(hPrinter, unmanagedBytes, pBytes.Length, out dwWritten))
                {
                    int err = Marshal.GetLastWin32Error();
                    Console.WriteLine($"WritePrinter fallo. BytesIntentados={pBytes.Length} Escrito={dwWritten} Error={err}");
                    success = false;
                }
                else
                {
                    success = (dwWritten == pBytes.Length);
                    if (!success)
                        Console.WriteLine($"WritePrinter escribió menos bytes. Esperado={pBytes.Length} Escrito={dwWritten}");
                }

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendBytesToPrinter excepción: {ex.Message}");
                success = false;
            }
            finally
            {
                if (unmanagedBytes != IntPtr.Zero)
                    Marshal.FreeHGlobal(unmanagedBytes);

                if (hPrinter != IntPtr.Zero)
                    ClosePrinter(hPrinter);
            }

            return success;
        }

        public static bool SendStringToPrinter(string printerName, string text, string encodingName = "IBM437")
        {
            Console.WriteLine(text);
            if (text == null) text = string.Empty;

            Encoding encoding;
            try
            {
                encoding = string.IsNullOrWhiteSpace(encodingName) ?
                    Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            byte[] bytes = encoding.GetBytes(text);
            return SendBytesToPrinter(printerName, bytes);
        }

        /// <summary>
        /// Convierte una cadena HEX a byte[]
        /// </summary>
        public static byte[] HexStringToByteArray(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return new byte[0];

            hex = hex.Replace(" ", "")
                    .Replace("-", "")
                    .Replace("\n", "")
                    .Replace("\r", "");

            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string debe tener longitud par");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        /// <summary>
        /// Envía texto con comandos de corte
        /// </summary>
        public static bool SendStringWithCut(string printerName, string text, string encodingName = "IBM437", string cutType = "full", int feedLines = 3)
        {
            if (text == null) text = string.Empty;
            Console.WriteLine(text);

            Encoding encoding;
            try
            {
                encoding = string.IsNullOrWhiteSpace(encodingName) ?
                    Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            List<byte> allBytes = new List<byte>();

            // Líneas antes del contenido
            for (int i = 0; i < feedLines; i++)
            {
                allBytes.AddRange(encoding.GetBytes("\n"));
            }

            allBytes.AddRange(encoding.GetBytes(text));

            // Líneas después del contenido
            for (int i = 0; i < feedLines; i++)
            {
                allBytes.AddRange(encoding.GetBytes("\n"));
            }

            // Comandos de corte ESC/POS
            allBytes.Add(0x1B); // ESC
            allBytes.Add(0x64); // d
            allBytes.Add(0x02); // Alimentar 2 líneas más

            if (cutType.ToLower() == "partial")
            {
                allBytes.Add(0x1B);
                allBytes.Add(0x6D);
            }
            else
            {
                allBytes.Add(0x1D);
                allBytes.Add(0x56);
                allBytes.Add(0x00);
            }

            return SendBytesToPrinter(printerName, allBytes.ToArray());
        }

        /// <summary>
        /// Envía bytes con comandos de corte
        /// </summary>
        public static bool SendBytesWithCut(string printerName, byte[] bytes, string cutType = "full", int feedLines = 3)
        {
            if (bytes == null) bytes = new byte[0];

            List<byte> allBytes = new List<byte>();
            allBytes.AddRange(bytes);

            // Alimentación de líneas (LF = 0x0A)
            for (int i = 0; i < feedLines; i++)
            {
                allBytes.Add(0x0A);
            }

            // Comandos de corte ESC/POS
            allBytes.Add(0x1B); // ESC
            allBytes.Add(0x64); // d
            allBytes.Add(0x02); // Alimentar 2 líneas más

            if (cutType.ToLower() == "partial")
            {
                allBytes.Add(0x1B);
                allBytes.Add(0x6D);
            }
            else
            {
                allBytes.Add(0x1D);
                allBytes.Add(0x56);
                allBytes.Add(0x00);
            }

            return SendBytesToPrinter(printerName, allBytes.ToArray());
        }
    }
}