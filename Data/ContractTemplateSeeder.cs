using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Data
{
    public static class ContractTemplateSeeder
    {
        public static async Task SeedTemplatesAsync(ApplicationDbContext context)
        {
            // Check if templates already exist
            if (context.ContractTemplates.Any())
            {
                return;
            }

            var templates = new List<ContractTemplate>
            {
                new ContractTemplate
                {
                    Name = "Wedding Photography Contract",
                    Category = ContractTemplateCategory.Wedding,
                    Description = "Comprehensive wedding photography contract including coverage details, deliverables, and payment terms.",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    Content = @"WEDDING PHOTOGRAPHY AGREEMENT

This agreement is made on {CurrentDate} between:

PHOTOGRAPHER: [Your Business Name]
Address: [Your Address]
Phone: [Your Phone]
Email: [Your Email]

CLIENT: {ClientFullName}
Address: {ClientAddress}
Phone: {ClientPhone}
Email: {ClientEmail}

WEDDING DETAILS:
Event Date: __________
Event Location: __________
Coverage Hours: __________
Additional Locations: __________

SERVICES PROVIDED:
1. Professional photography coverage for the agreed hours
2. Post-production editing and retouching
3. Online gallery delivery within 6-8 weeks
4. High-resolution digital images
5. Personal printing rights

DELIVERABLES:
- Minimum of [X] edited high-resolution images
- Online gallery with download access
- Personal printing rights for non-commercial use

PAYMENT TERMS:
Total Package Price: $__________
Deposit (50%): $__________  Due: __________
Balance (50%): $__________  Due: 2 weeks before event

CANCELLATION POLICY:
The deposit is non-refundable. If the Client cancels within 90 days of the event, 50% of the balance is due. If cancellation occurs within 30 days, the full balance is due.

COPYRIGHT:
The Photographer retains copyright to all images. The Client receives a personal-use license for the delivered images.

RESCHEDULING:
One free reschedule is permitted with 60+ days notice. Additional reschedules may incur a $200 fee.

FORCE MAJEURE:
Neither party shall be liable for failure to perform due to circumstances beyond their control.

AGREEMENTS:
By signing below, both parties agree to the terms outlined in this contract.

_______________________          Date: __________
Photographer Signature

_______________________          Date: __________
Client Signature: {ClientFullName}

Copyright © {CurrentYear}. All rights reserved."
                },

                new ContractTemplate
                {
                    Name = "Portrait Photography Contract",
                    Category = ContractTemplateCategory.Portrait,
                    Description = "Standard portrait photography session contract for individuals and families.",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    Content = @"PORTRAIT PHOTOGRAPHY SESSION AGREEMENT

Date: {CurrentDate}

PHOTOGRAPHER: [Your Business Name]
Contact: [Your Email] | [Your Phone]

CLIENT INFORMATION:
Name: {ClientFullName}
Email: {ClientEmail}
Phone: {ClientPhone}
Address: {ClientAddress}

SESSION DETAILS:
Session Date: __________
Session Time: __________
Location: __________
Session Duration: [X] hour(s)
Number of Subjects: __________

PACKAGE INCLUDES:
- Professional portrait session
- [X] outfit changes
- Post-production editing
- [X] edited high-resolution digital images
- Online gallery for 90 days
- Personal printing rights

INVESTMENT:
Session Fee: $__________
Payment Due: At time of booking

PREPARATION:
Please arrive 15 minutes early. Wear comfortable clothing. Bring any props or accessories you'd like to include.

IMAGE DELIVERY:
Edited images will be delivered via online gallery within 2 weeks of the session date.

USAGE RIGHTS:
Client receives personal-use rights. Images may not be used for commercial purposes without written permission.

PHOTOGRAPHER'S RIGHTS:
Photographer reserves the right to use images for portfolio, marketing, and social media with client's permission.

WEATHER POLICY (Outdoor Sessions):
In case of inclement weather, the session will be rescheduled to a mutually agreed upon date.

CANCELLATION:
Cancellations made 48+ hours in advance will receive a full refund. Late cancellations forfeit 50% of session fee.

AGREEMENT:
I have read and agree to the terms of this portrait photography contract.

_______________________          Date: __________
Photographer Signature

_______________________          Date: __________
Client Signature: {ClientFullName}

© {CurrentYear} [Your Business Name]. All rights reserved."
                },

                new ContractTemplate
                {
                    Name = "Event Photography Contract",
                    Category = ContractTemplateCategory.Event,
                    Description = "Professional event photography contract for corporate events, parties, and special occasions.",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    Content = @"EVENT PHOTOGRAPHY AGREEMENT

Agreement Date: {CurrentDate}

SERVICE PROVIDER: [Your Business Name]
[Your Address]
[Your Phone] | [Your Email]

CLIENT: {ClientFullName}
Email: {ClientEmail}
Phone: {ClientPhone}

EVENT INFORMATION:
Event Name: __________
Event Date: __________
Event Time: __________ to __________
Event Location: __________
Event Type: __________

PHOTOGRAPHY SERVICES:
Coverage Duration: ____ hours
Number of Photographers: ____
Coverage Style: Candid / Posed / Mixed

DELIVERABLES:
- Professional event photography coverage
- [X] edited high-resolution images
- Online gallery with download capability
- Delivery within 3 weeks of event date
- Copyright license for business use

FEES AND PAYMENT:
Photography Fee: $__________
Travel Fee (if applicable): $__________
Total Investment: $__________

Payment Schedule:
- 50% Deposit: $__________ (Due at signing)
- 50% Balance: $__________ (Due 7 days before event)

ADDITIONAL COSTS:
Overtime (beyond agreed hours): $____ per hour
Additional photographers: $____ per photographer
Rush delivery (within 1 week): $____

USAGE LICENSE:
Client receives full commercial usage rights for all delivered images. Photographer retains copyright and may use images for portfolio purposes.

CLIENT RESPONSIBILITIES:
- Provide event timeline and key moments to photograph
- Designate a point of contact during the event
- Ensure photographer has meals/breaks during long events

CANCELLATION POLICY:
Deposit is non-refundable. Balance due if cancellation occurs within 14 days of event.

LIMITATION OF LIABILITY:
Photographer will take reasonable care but is not liable for missed shots due to circumstances beyond their control.

AGREEMENT:
Both parties agree to the terms and conditions outlined above.

_______________________          Date: __________
{ClientFullName}
Client Signature

_______________________          Date: __________
Photographer Signature

Document ID: Contract-{CurrentDate}
© {CurrentYear} All Rights Reserved"
                },

                new ContractTemplate
                {
                    Name = "Commercial Photography Contract",
                    Category = ContractTemplateCategory.Commercial,
                    Description = "Commercial photography contract for product, advertising, and business photography.",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    Content = @"COMMERCIAL PHOTOGRAPHY AGREEMENT

Effective Date: {CurrentDate}

PHOTOGRAPHER: [Your Business Name]
[Business Address]
Email: [Your Email]
Phone: [Your Phone]

CLIENT: {ClientFullName}
Company: __________
Address: {ClientAddress}
Email: {ClientEmail}
Phone: {ClientPhone}

PROJECT DETAILS:
Project Name: __________
Photography Type: □ Product  □ Advertising  □ Corporate  □ Other: ____
Shoot Date(s): __________
Location: __________

SCOPE OF WORK:
- Number of final images: ____
- Image format: □ High-res JPEG  □ RAW  □ Both
- Post-production: □ Basic editing  □ Advanced retouching  □ Custom
- Specific requirements: __________

DELIVERABLES:
- [X] edited high-resolution images
- Format: JPEG / TIFF / RAW
- Delivery method: Online transfer / USB drive
- Timeline: Within [X] business days

USAGE RIGHTS & LICENSING:
□ Exclusive Rights - Client has sole usage rights (additional fee applies)
□ Non-Exclusive Rights - Both parties may use images
□ Limited Usage - Specify: __________
□ Unlimited Commercial Usage

Duration of License: □ Perpetual  □ [X] years  □ Other: ____
Geographic Rights: □ Worldwide  □ [Country/Region]: ____
Media: □ All Media  □ Specific: ____

INVESTMENT:
Photography Fee: $__________
Usage License: $__________
Post-Production: $__________
Travel/Expenses: $__________
TOTAL: $__________

PAYMENT TERMS:
- 50% deposit due upon signing: $__________
- 50% due upon delivery: $__________
- Payment methods: Check, Bank transfer, Credit card

EXPENSES:
Client agrees to reimburse reasonable expenses including travel, accommodation, equipment rental, and permits if required.

COPYRIGHT:
Photographer retains copyright to all images. Client receives usage rights as specified in this agreement.

REVISIONS:
Included: [X] rounds of revisions
Additional revisions: $____ per hour

CANCELLATION:
If Client cancels within 7 days of shoot, 100% of deposit is forfeited. Cancellations with 7+ days notice forfeit 50%.

INDEMNIFICATION:
Client indemnifies Photographer against any claims arising from Client's use of the images beyond the granted license.

CONFIDENTIALITY:
Both parties agree to keep confidential any proprietary information shared during this engagement.

CREDIT:
Photographer requests credit as "[Your Name] / [Your Business Name]" when images are published.

MODIFICATIONS:
Any modifications to this agreement must be made in writing and signed by both parties.

ACCEPTANCE:
By signing below, both parties accept and agree to these terms.

_______________________          Date: __________
{ClientFullName}
Client Signature

_______________________          Date: __________
Photographer Signature

Contract Reference: {CurrentYear}-____
© {CurrentYear} [Your Business Name]. All Rights Reserved."
                },

                new ContractTemplate
                {
                    Name = "General Photography Contract",
                    Category = ContractTemplateCategory.General,
                    Description = "Flexible general-purpose photography contract suitable for various photography services.",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    Content = @"PHOTOGRAPHY SERVICES AGREEMENT

Date: {CurrentDate}

PHOTOGRAPHER:
Business Name: [Your Business Name]
Address: [Your Address]
Phone: [Your Phone]
Email: [Your Email]

CLIENT:
Name: {ClientFullName}
Address: {ClientAddress}
Phone: {ClientPhone}
Email: {ClientEmail}

SERVICE DETAILS:
Service Type: __________
Date of Service: __________
Location: __________
Duration: __________

SERVICES PROVIDED:
The Photographer agrees to provide the following services:
- Professional photography coverage
- Post-production editing
- Digital image delivery
- [Additional services as agreed]

DELIVERABLES:
- Number of edited images: [X]
- File format: High-resolution JPEG
- Delivery method: Online gallery
- Delivery timeline: [X] weeks from service date

FEES:
Total Service Fee: $__________
Deposit (50%): $__________  Due: Upon signing
Balance (50%): $__________  Due: [Date/Upon delivery]

PAYMENT TERMS:
- Accepted payment methods: Cash, Check, Credit Card, Bank Transfer
- Late payment fee: 5% per month on overdue balance
- Deposit is non-refundable

CANCELLATION & RESCHEDULING:
- Cancellations with 14+ days notice: Deposit refunded minus $100 booking fee
- Cancellations with less than 14 days notice: Deposit forfeited
- One free reschedule allowed with 30+ days notice
- Additional reschedules: $150 fee

COPYRIGHT & USAGE:
- Photographer retains all copyright to images
- Client receives personal-use license for delivered images
- Commercial use requires written permission and additional licensing fee
- Client may not resell, redistribute, or claim authorship of images

PHOTOGRAPHER'S RIGHTS:
Photographer reserves the right to use images for:
- Portfolio and marketing materials
- Website and social media
- Photography competitions and publications

CLIENT RESPONSIBILITIES:
- Provide accurate event/session details
- Arrive on time for scheduled sessions
- Cooperate and follow photographer's direction
- Provide suitable environment and lighting conditions when applicable

LIMITATION OF LIABILITY:
While every effort will be made to capture requested shots, the Photographer is not liable for missed photographs due to circumstances beyond their control including but not limited to weather, late arrivals, or obstructions.

MODEL RELEASE:
Client grants permission for Photographer to use photographs for promotional purposes unless they opt out in writing.

FORCE MAJEURE:
Neither party shall be held liable for failure to perform due to causes beyond their reasonable control.

ENTIRE AGREEMENT:
This agreement constitutes the entire understanding between parties and supersedes all prior agreements.

ACCEPTANCE:
I have read, understand, and agree to the terms and conditions of this photography agreement.

_______________________          Date: __________
Client Signature: {ClientFullName}

_______________________          Date: __________
Photographer Signature

© {CurrentYear} [Your Business Name]"
                }
            };

            context.ContractTemplates.AddRange(templates);
            await context.SaveChangesAsync();
        }
    }
}
