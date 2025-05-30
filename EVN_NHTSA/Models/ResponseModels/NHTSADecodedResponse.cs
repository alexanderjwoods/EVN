using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVN_NHTSA.Models.ResponseModels
{
	public class NHTSADecodedResponse
	{
		public int Count { get; set; }

		public string Message { get; set; } = string.Empty;

		public string SearchCriteria { get; set; } = string.Empty;

		public IEnumerable<NHTSAResult?> Results { get; set; } = [];
	}

	public class NHTSAResult
	{
		public string? Value { get; set; }

		public string? ValueId { get; set; }

		public string? Variable { get; set; }

		public int? VariableId { get; set; }
	}
}
