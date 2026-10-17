# BandFounder

A place for musicians to find each other, form bands, and talk in chatrooms.

## Language

**Account**:
The logged-in identity. It has one current email address.
_Avoid_: User, member (except as a chatroom member)

**Email address**:
The inbox currently stored on an Account. Verification is about this address, not about the Account existing.
_Avoid_: Email (when you mean the Account), verified user

**Verified**:
The Account's current email address has been proven. In the code this will be `EmailVerifiedAt` set to a time.
_Avoid_: Confirmed, active, validated

**Address-proving mail**:
Mail whose job is to prove or recover an email address. Today that is verification and password reset. It may go to an unverified address.
_Avoid_: Transactional email (too wide), auth email

**Product mail**:
Every other email the app sends. Today that is message notification mail. It must not go to an unverified address.
_Avoid_: Notification (when you mean all gated mail), marketing

**Music profile**:
The artists and genre weights that represent an Account's music.
_Avoid_: taste

**Linked playlist**:
A public Spotify playlist an Account has imported. Its artists feed the music profile.

**Artist source**:
Why an Artist is on an Account: Spotify sign-in, manual add, or a Linked playlist.
