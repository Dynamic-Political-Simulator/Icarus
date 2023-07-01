using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Icarus.Context.Models
{
	public class StaffAction
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long StaffActionId { get; set; }

		public string ActionTitle { get; set; }
		public string ActionDescription { get; set; }
		public string ActionResponse { get; set; }

		public StaffActionStatus Status { get; set; }

		public string SubmitterId { get; set; }
		public virtual DiscordUser Submitter { get; set; }

		public string AssignedToId { get; set; }
		public virtual DiscordUser AssignedTo { get; set; }

		public string MessageId { get; set; }
	}

	public enum StaffActionStatus
	{
		TODO,
		IN_PROGRESS,
		DONE
	}
}