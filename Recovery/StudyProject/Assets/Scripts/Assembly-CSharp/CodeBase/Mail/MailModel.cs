using System;
using System.Collections.Generic;
using Codebase.Cashback;
using Codebase.Core;
using Codebase.Rewards;
using Codebase.Services.Time;
using Codebase.Unit;
using UniMob;
using UniMob.Core;

namespace Codebase.Mail
{
	public class MailModel : Model
	{
		private static readonly Dictionary<string, string> ReasonLocalizationKeys;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private CashbackModel cashbackModel;

		private ComputedAtom<GameTime> __ExpirationTime;

		private ComputedAtom<GameTime> __ReceiveTime;

		private ComputedAtom<float> __SecondsExist;

		private ComputedAtom<float> __SecondsToExpire;

		private ComputedAtom<bool> __CanClaim;

		private ComputedAtom<bool> __IsExpired;

		private ComputedAtom<bool> __IsDeleted;

		private ComputedAtom<bool> __IsViewed;

		private ComputedAtom<bool> __IsClaimed;

		private ComputedAtom<bool> __ArchiveOnClaim;

		private ComputedAtom<int> __Gems;

		private ComputedAtom<int> __InAppValue;

		private ComputedAtom<int> __BoosterTokens;

		private ComputedAtom<string> __BoosterTokenProductId;

		private ComputedAtom<List<string>> __Attachments;

		private ComputedAtom<List<string>> __TextArgs;

		private ComputedAtom<List<StatModifier>> __StatRewards;

		private ComputedAtom<bool> __IsInteractable;

		public MailUd Data { get; }

		public string Guid => null;

		public string Key => null;

		private string LocalizationKey => null;

		public string TitleLocalizationKey => null;

		public string ShortTextLocalizationKey => null;

		public string LongTextLocalizationKey => null;

		public bool IsCashback => false;

		public bool HasDescription => false;

		private string LocalizedDescription => null;

		public string TitleText => null;

		public string ShortText => null;

		public string LongText => null;

		public GameTime ExpirationTime
		{
			get
			{
				return default;
			}
			set
			{
			}
		}

		public GameTime ReceiveTime
		{
			get
			{
				return default;
			}
			set
			{
			}
		}

		public float SecondsExist => 0f;

		public float SecondsToExpire => 0f;

		public bool CanClaim => false;

		public bool IsExpired => false;

		public bool IsDeleted => false;

		public bool IsViewed
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool IsClaimed
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool ArchiveOnClaim
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool IsArchived => false;

		public int Gems
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int InAppValue
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public int BoosterTokens
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public string BoosterTokenProductId
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public List<string> Attachments => null;

		public List<string> TextArgs => null;

		public List<StatModifier> StatRewards => null;

		public bool IsInteractable => false;

		public List<RewardBase> ParsedRewards => null;

		public MailModel(Lifetime lifetime, MailUd ud)
			: base(default)
		{
		}

		public void MarkDeleted()
		{
		}

		public void SetAttachments(List<string> attachments)
		{
		}

		public void SetTextArgs(List<string> textArgs)
		{
		}

		public void AddReward(RewardBase reward)
		{
		}

		public void SetRewards(List<string> rewards)
		{
		}

		public void SetStatRewards(List<StatModifier> rewards)
		{
		}

		public void Claim(Action onComplete = null)
		{
		}
	}
}
