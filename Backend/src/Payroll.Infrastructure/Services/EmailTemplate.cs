namespace Payroll.Infrastructure.Services;

/// <summary>Wraps plain HTML content in the single shared Payroll SA blue-themed email layout (header/body/footer).</summary>
public static class EmailTemplate
{
    public static string Render(string title, string bodyHtml)
    {
        var year = DateTime.UtcNow.Year;
        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
            <body style="margin:0;padding:0;background:#f4f6fb;font-family:'Segoe UI',Arial,sans-serif;color:#212529;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6fb;padding:32px 16px;">
                    <tr><td align="center">
                        <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="width:480px;max-width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 8px 24px rgba(13,110,253,0.12);">
                            <tr><td style="background:#0d6efd;padding:24px 32px;text-align:center;">
                                <span style="color:#ffffff;font-size:20px;font-weight:800;letter-spacing:.02em;">Payroll SA</span>
                            </td></tr>
                            <tr><td style="padding:32px;">
                                <h1 style="margin:0 0 16px;font-size:19px;font-weight:700;color:#0a58ca;">{title}</h1>
                                <div style="font-size:14px;line-height:1.6;color:#212529;">{bodyHtml}</div>
                            </td></tr>
                            <tr><td style="background:#f8f9fa;padding:16px 32px;text-align:center;border-top:1px solid #e9ecef;">
                                <p style="margin:0;font-size:12px;color:#6c757d;">&copy; {year} Payroll SA. All rights reserved.</p>
                                <p style="margin:4px 0 0;font-size:12px;color:#6c757d;">This is an automated message &mdash; please do not reply.</p>
                            </td></tr>
                        </table>
                    </td></tr>
                </table>
            </body>
            </html>
            """;
    }
}
