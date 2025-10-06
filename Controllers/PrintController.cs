

using System.Text;
using Microsoft.AspNetCore.Mvc;
using GaiaPrintAPI.Models;
using GaiaPrintAPI.Helpers;
using System.Drawing.Printing;
using System.Collections.Generic; 
using System;

namespace GaiaPrintAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly ILogger<PrintController> _logger;

        public PrintController(ILogger<PrintController> logger)
        {
            _logger = logger;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "API de Impresión Gaia funcionando",
                Data = new { timestamp = DateTime.Now }
            });
        }

        [HttpGet("printers")]
        public IActionResult GetAvailablePrinters()
        {
            try
            {
                var printers = new List<PrinterInfo>();
                var defaultPrinter = new PrinterSettings().PrinterName;

                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    printers.Add(new PrinterInfo
                    {
                        Name = printer,
                        IsDefault = printer == defaultPrinter
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Impresoras obtenidas correctamente",
                    Data = printers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo impresoras");
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Error obteniendo impresoras: {ex.Message}"
                });
            }
        }

        [HttpPost("receipt")]
        public IActionResult PrintReceipt([FromBody] PrintRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Request vacía"
                });
            }

            if (string.IsNullOrWhiteSpace(request.PrinterName))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "PrinterName requerido"
                });
            }

            try
            {
                // Normalizar valores
                string contentType = (request.ContentType ?? "").Trim().ToUpperInvariant();
                bool cutPaper = request.CutPaper ?? false;
                string cutType = (request.CutType ?? "full").Trim().ToLowerInvariant();
                int feedLines = request.FeedLines ?? 3;
                string encodingName = string.IsNullOrWhiteSpace(request.EncodingName) ?
                    "IBM437" : request.EncodingName;

                bool success = false;

                switch (contentType)
                {
                    case "ESC_POS_HEX":
                        byte[] payloadBytes = RawPrinterHelper.HexStringToByteArray(request.Payload ?? "");
                        success = cutPaper ?
                            RawPrinterHelper.SendBytesWithCut(request.PrinterName, payloadBytes, cutType, feedLines) :
                            RawPrinterHelper.SendBytesToPrinter(request.PrinterName, payloadBytes);
                        break;

                    case "ESC_POS_TEXT":
                        success = cutPaper ?
                            RawPrinterHelper.SendStringWithCut(request.PrinterName, request.Payload ?? "", encodingName, cutType, feedLines) :
                            RawPrinterHelper.SendStringToPrinter(request.PrinterName, request.Payload ?? "", encodingName);
                        break;

                    default:
                        byte[] data = Encoding.GetEncoding(encodingName).GetBytes(request.Payload ?? "");
                        success = cutPaper ?
                            RawPrinterHelper.SendBytesWithCut(request.PrinterName, data, cutType, feedLines) :
                            RawPrinterHelper.SendBytesToPrinter(request.PrinterName, data);
                        break;
                }

                if (!success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Error en la impresión. Verifique la impresora y los permisos."
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Impresión completada correctamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PrintReceipt");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error de impresión: {ex.Message}"
                });
            }
        }

        [HttpPost("test-print")]
        public IActionResult TestPrint([FromBody] PrintRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PrinterName))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "PrinterName requerido"
                });
            }

            var testContent = @"=== TIENDA Prueba  ===
        
                Producto        Cant    Total
                ----------------------------
                producto        2       $40
                producto        1       $25
                producto        1       $35
                ----------------------------
                TOTAL: $100

                ¡Gracias por su compra!";

            var testRequest = new PrintRequest
            {
                PrinterName = request.PrinterName,
                ContentType = "ESC_POS_TEXT",
                Payload = testContent,
                CutPaper = true,
                CutType = "full",
                FeedLines = 3,
                EncodingName = "IBM437"
            };

            return PrintReceipt(testRequest);
        }

        [HttpPost("raw-bytes")]
        public IActionResult PrintRawBytes([FromBody] PrintRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PrinterName))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "PrinterName requerido"
                });
            }

            try
            {
                byte[] bytes = RawPrinterHelper.HexStringToByteArray(request.Payload ?? "");
                bool success = RawPrinterHelper.SendBytesToPrinter(request.PrinterName, bytes);

                if (!success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Error enviando bytes a la impresora"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Bytes enviados correctamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PrintRawBytes");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
    }
}
