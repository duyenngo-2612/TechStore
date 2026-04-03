using System.Net;
using System.Net.Mail;

namespace TechStore.Services
{
    // Thêm ": IEmailService" để Program.cs hiểu được mối liên kết
    public class EmailSender : IEmailService
    {
        private readonly string _fromEmail = "duyenngo.31241027639@st.ueh.edu.vn"; // Thay email thật của bạn
        private readonly string _emailPassword = "nqzueynnuqamtzec"; // thay 16 kí tự thật vào đây 

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential(_fromEmail, _emailPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail, "TechStore System"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(toEmail);

                    // Sử dụng SendMailAsync thay vì Send
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi ra console để debug khi cần
                Console.WriteLine("Lỗi gửi mail: " + ex.Message);
                throw; // Ném lỗi để Controller bắt được và hiện thông báo
            }
        }
    }
}