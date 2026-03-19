## GDPR & Privacy Compliance Guidelines

### Privacy Policy & User Consent

All registration forms (MAUI and Razor Pages) must include:
- **Explicit Privacy Policy Link**: Direct link to `/Home/Privacy` or in-app privacy page
- **Separate Checkboxes**: 
  - ? Accept Terms of Service (link to terms page)
  - ? Accept Privacy Policy (link to privacy page)
- **No Pre-checked Consent**: Users must actively opt-in; pre-checked boxes violate GDPR

### Data Deletion (Right to be Forgotten)

Users must have the ability to request account deletion through:
- Account management page/settings
- User profile section (authenticated users)
- Request form accessible after login

### Required Fields in ApplicationUser

Track GDPR-related metadata:
```csharp
public DateTime? ConsentGivenDate { get; set; }    // When user consented
public bool PrivacyConsentGiven { get; set; }       // Privacy policy consent
public bool TermsConsentGiven { get; set; }         // Terms of service consent
public bool MarketingOptIn { get; set; }            // Marketing communications
```

### Privacy Audit Trail

All consent and deletion requests must be logged with:
- Timestamp
- User ID
- Consent type
- IP address (for audit purposes)

### Localization

All privacy and terms content must support multi-language localization (nl, en, fr) via `SharedResources.resx` files.

### Implementation Checklist

- [ ] Add privacy policy consent checkbox to MAUI RegisterPage
- [ ] Add privacy policy consent checkbox to Razor Register.cshtml
- [ ] Implement data deletion API endpoint
- [ ] Create user deletion confirmation flow
- [ ] Add GDPR tracking fields to ApplicationUser
- [ ] Create Terms of Service page
- [ ] Log all consent and deletion requests
- [ ] Update RegisterViewModel and RegisterModel validators