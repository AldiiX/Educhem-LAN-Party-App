using server.Data.Entities;

namespace server.Dto.Mappers;

public static class ProblemReportMapper {
	extension(ProblemReport report) {
		public ProblemReportDto ToDto(bool deep = true) {
			return new ProblemReportDto {
				Id = report.Id,
				CreatedAtUtc = report.CreatedAtUtc,
				UpdatedAtUtc = report.UpdatedAtUtc,
				Category = report.Category,
				Priority = report.Priority,
				Status = report.Status,
				Title = report.Title,
				Description = report.Description,
				Contact = report.Contact,
				ResolutionNote = report.ResolutionNote,
				ResolvedAtUtc = report.ResolvedAtUtc,
				Reporter = report.Reporter.ToProfileDto(false),
				ResolvedBy = report.ResolvedBy?.ToProfileDto(false),
			};
		}
	}
}
