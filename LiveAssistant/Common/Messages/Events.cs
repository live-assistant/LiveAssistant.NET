//    Copyright (C) 2023  Live Assistant official Windows app Authors
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CommunityToolkit.Mvvm.Messaging.Messages;
using LiveAssistant.Database;
using Microsoft.UI.Xaml.Controls;

namespace LiveAssistant.Common.Messages;

public class ShowContentDialogMessage : ValueChangedMessage<ContentDialog>
{
    public ShowContentDialogMessage(ContentDialog value) : base(value) { }
}

public class ShowInfoBarMessage : ValueChangedMessage<InfoBar>
{
    public ShowInfoBarMessage(InfoBar value) : base(value) { }
}

internal class AudienceUpdateEventMessage : ValueChangedMessage<Audience>
{
    public AudienceUpdateEventMessage(Audience value) : base(value) { }
}

internal class EnterEventMessage : ValueChangedMessage<Enter>
{
    public EnterEventMessage(Enter value) : base(value) { }
}

internal class FollowEventMessage : ValueChangedMessage<Follow>
{
    public FollowEventMessage(Follow value) : base(value) { }
}

internal class MessageEventMessage : ValueChangedMessage<Message>
{
    public MessageEventMessage(Message value) : base(value) { }
}

internal class SuperChatEventMessage : ValueChangedMessage<SuperChat>
{
    public SuperChatEventMessage(SuperChat value) : base(value) { }
}

internal class GiftEventMessage : ValueChangedMessage<Gift>
{
    public GiftEventMessage(Gift value) : base(value) { }
}

internal class MembershipEventMessage : ValueChangedMessage<Membership>
{
    public MembershipEventMessage(Membership value) : base(value) { }
}

internal class ViewersCountEventMessage : ValueChangedMessage<ViewersCount>
{
    public ViewersCountEventMessage(ViewersCount value) : base(value) { }
}

internal class CaptionEventMessage : ValueChangedMessage<Caption>
{
    public CaptionEventMessage(Caption value) : base(value) { }
}

internal class HeartRateEventMessage : ValueChangedMessage<HeartRate>
{
    public HeartRateEventMessage(HeartRate value) : base(value) { }
}
