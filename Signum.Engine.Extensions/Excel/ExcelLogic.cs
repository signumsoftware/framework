using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Excel;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Signum.Utilities;
using System.IO;
using Signum.Engine.Operations;
using Signum.Engine.Mailing;
using Signum.Entities.Mailing;
using Signum.Engine.UserQueries;
using Signum.Entities.UserAssets;

namespace Signum.Engine.Excel
{
    public static class ExcelLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool excelReport, bool userQueryExcel)
        {
            if (excelReport)
            {
                QueryLogic.Start(sb);

                sb.Include<ExcelReportEntity>();
                dqm.RegisterQuery(typeof(ExcelReportEntity), () =>
                    from s in Database.Query<ExcelReportEntity>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Query,
                        s.File.FileName,
                        s.DisplayName,
                    });

                new Graph<ExcelReportEntity>.Execute(ExcelReportOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (er, _) => { }
                }.Register();

                new Graph<ExcelReportEntity>.Delete(ExcelReportOperation.Delete)
                {
                    Lite = true,
                    Delete = (er, _) => { er.Delete(); }
                }.Register();
            }

            if (userQueryExcel)
            {
                sb.Include<ExcelAttachmentEntity>();
                dqm.RegisterQuery(typeof(ExcelAttachmentEntity), () =>
                    from s in Database.Query<ExcelAttachmentEntity>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.FileName,
                        s.UserQuery,
                        s.Related,
                    });

                new Graph<ExcelAttachmentEntity>.Execute(ExcelAttachmentOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (er, _) => { }
                }.Register();


                EmailTemplateLogic.GenerateAttachment.Register((ExcelAttachmentEntity uqe, EmailTemplateEntity template, IEntity entity) =>
                {
                    var finalEntity = uqe.Related?.Retrieve() ?? (Entity)entity;

                    using (finalEntity == null ? null : CurrentEntityConverter.SetCurrentEntity(finalEntity))
                    {
                        QueryRequest request = UserQueryLogic.ToQueryRequest(uqe.UserQuery.Retrieve());

                        var bytes = ExcelLogic.ExecutePlainExcel(request);

                        return new List<EmailAttachmentEntity>
                        {
                            new EmailAttachmentEntity
                            {
                                File = Files.EmbeddedFilePathLogic.SaveFile(new Entities.Files.EmbeddedFilePathEntity(EmailFileType.Attachment, uqe.FileName, bytes)),
                                Type = EmailAttachmentType.Attachment,
                            }
                        };
                    }
                });
            }
        }

        public static List<Lite<ExcelReportEntity>> GetExcelReports(object queryName)
        {
            return (from er in Database.Query<ExcelReportEntity>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName)
                    select er.ToLite()).ToList();
        }

        public static byte[] ExecuteExcelReport(Lite<ExcelReportEntity> excelReport, QueryRequest request)
        {
            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);

            ExcelReportEntity report = excelReport.RetrieveAndForget();
            string extension = Path.GetExtension(report.File.FileName);
            if (extension != ".xlsx")
                throw new ApplicationException(ExcelMessage.ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0.NiceToString().FormatWith(extension));

            return ExcelGenerator.WriteDataInExcelFile(queryResult, report.File.BinaryFile);
        }

        public static byte[] ExecutePlainExcel(QueryRequest request)
        {
            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);

            return PlainExcelGenerator.WritePlainExcel(queryResult);
        }
    }
}
