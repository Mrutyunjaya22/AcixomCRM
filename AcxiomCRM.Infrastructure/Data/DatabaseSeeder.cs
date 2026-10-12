using AcxiomCRM.Core.Constants;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AcxiomCRM.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        try
        {
            // 1. Seed Roles
            foreach (var roleName in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation("Seeded role: {Role}", roleName);
                }
            }

            // 2. Seed Users
            var adminEmail = "admin@acxiomcrm.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
            }

            var managerEmail = "manager@acxiomcrm.local";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = "Sarah Connor (Sales Manager)",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-5)
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, Roles.Manager);
                }
            }

            var sales1Email = "sales1@acxiomcrm.local";
            var sales1User = await userManager.FindByEmailAsync(sales1Email);
            if (sales1User == null)
            {
                sales1User = new ApplicationUser
                {
                    UserName = sales1Email,
                    Email = sales1Email,
                    FullName = "Alex Rivera (Sales Exec)",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-4)
                };
                var result = await userManager.CreateAsync(sales1User, "Sales@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales1User, Roles.SalesExecutive);
                }
            }

            // Also seed sales@acxiomcrm.local alias to support standard demo credentials
            var salesDefaultEmail = "sales@acxiomcrm.local";
            var salesDefaultUser = await userManager.FindByEmailAsync(salesDefaultEmail);
            if (salesDefaultUser == null)
            {
                salesDefaultUser = new ApplicationUser
                {
                    UserName = salesDefaultEmail,
                    Email = salesDefaultEmail,
                    FullName = "Alex Rivera (Sales Exec)",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-4)
                };
                var result = await userManager.CreateAsync(salesDefaultUser, "Sales@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesDefaultUser, Roles.SalesExecutive);
                }
            }

            var sales2Email = "sales2@acxiomcrm.local";
            var sales2User = await userManager.FindByEmailAsync(sales2Email);
            if (sales2User == null)
            {
                sales2User = new ApplicationUser
                {
                    UserName = sales2Email,
                    Email = sales2Email,
                    FullName = "Elena Rostova (Sales Exec)",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                };
                var result = await userManager.CreateAsync(sales2User, "Sales@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales2User, Roles.SalesExecutive);
                }
            }

            // 3. Seed Customers if empty
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-00001",
                        CustomerName = "Acme Industrial Corp",
                        Email = "procurement@acmeind.com",
                        Phone = "9876543210",
                        CompanyName = "Acme Industries",
                        Address = "100 Industrial Boulevard",
                        City = "Chicago",
                        State = "Illinois",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-4),
                        CreatedById = adminUser.Id,
                        AssignedToId = sales1User.Id,
                        Notes = "Enterprise customer with multi-location operations."
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-00002",
                        CustomerName = "Global Logistics Ltd",
                        Email = "contact@globallogistics.org",
                        Phone = "9876543211",
                        CompanyName = "Global Logistics",
                        Address = "45 Harbor View Road",
                        City = "Seattle",
                        State = "Washington",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-3),
                        CreatedById = managerUser.Id,
                        AssignedToId = sales2User.Id,
                        Notes = "Key supply chain partner."
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-00003",
                        CustomerName = "Apex FinTech Solutions",
                        Email = "info@apexfintech.io",
                        Phone = "9876543212",
                        CompanyName = "Apex FinTech",
                        Address = "742 Financial Square",
                        City = "New York",
                        State = "New York",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedById = adminUser.Id,
                        AssignedToId = sales1User.Id,
                        Notes = "High-growth digital banking provider."
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-00004",
                        CustomerName = "Nexus Health Systems",
                        Email = "partnerships@nexushealth.org",
                        Phone = "9876543213",
                        CompanyName = "Nexus Health",
                        Address = "300 Medical Center Drive",
                        City = "Boston",
                        State = "Massachusetts",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        CreatedById = managerUser.Id,
                        AssignedToId = sales2User.Id,
                        Notes = "Hospital network evaluating clinical workflow software."
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-00005",
                        CustomerName = "Vanguard Retailers",
                        Email = "vendor@vanguardretail.com",
                        Phone = "9876543214",
                        CompanyName = "Vanguard Retail",
                        Address = "12 Market Place",
                        City = "Dallas",
                        State = "Texas",
                        Status = CustomerStatus.Inactive,
                        CreatedDate = DateTime.UtcNow.AddMonths(-5),
                        CreatedById = adminUser.Id,
                        AssignedToId = sales1User.Id,
                        Notes = "Deactivated due to account consolidation."
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            var c1 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-00001");
            var c2 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-00002");
            var c3 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-00003");
            var c4 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-00004");

            // 4. Seed Leads if empty
            if (!await context.Leads.AnyAsync())
            {
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-00001",
                        LeadName = "Starlight Technologies",
                        Email = "growth@starlighttech.com",
                        Phone = "9876543220",
                        CompanyName = "Starlight Tech",
                        Source = LeadSource.Website,
                        Status = LeadStatus.Qualified,
                        Priority = LeadPriority.High,
                        ExpectedValue = 75000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        AssignedToId = sales1User.Id,
                        Notes = "Interested in enterprise CRM migration."
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-00002",
                        LeadName = "Quantum Data Inc",
                        Email = "sales@quantumdata.ai",
                        Phone = "9876543221",
                        CompanyName = "Quantum Data",
                        Source = LeadSource.Referral,
                        Status = LeadStatus.Contacted,
                        Priority = LeadPriority.Medium,
                        ExpectedValue = 45000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedToId = sales2User.Id,
                        Notes = "Initial demo completed, proposal requested."
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-00003",
                        LeadName = "Zenith Media Group",
                        Email = "leads@zenithmedia.net",
                        Phone = "9876543222",
                        CompanyName = "Zenith Media",
                        Source = LeadSource.ColdCall,
                        Status = LeadStatus.New,
                        Priority = LeadPriority.Low,
                        ExpectedValue = 18000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-3),
                        AssignedToId = sales1User.Id,
                        Notes = "Awaiting callback from marketing head."
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-00004",
                        LeadName = "BlueWave Clean Energy",
                        Email = "contact@bluewaveenergy.org",
                        Phone = "9876543223",
                        CompanyName = "BlueWave Energy",
                        Source = LeadSource.Event,
                        Status = LeadStatus.Converted,
                        Priority = LeadPriority.High,
                        ExpectedValue = 90000.00m,
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        AssignedToId = sales2User.Id,
                        ConvertedCustomerId = c2.CustomerId,
                        Notes = "Converted into customer and won contract."
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-00005",
                        LeadName = "Legacy Freight LLC",
                        Email = "info@legacyfreight.biz",
                        Phone = "9876543224",
                        CompanyName = "Legacy Freight",
                        Source = LeadSource.SocialMedia,
                        Status = LeadStatus.Lost,
                        Priority = LeadPriority.Low,
                        ExpectedValue = 12000.00m,
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        AssignedToId = sales1User.Id,
                        Notes = "Selected competitor due to existing legacy contract."
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            var l1 = await context.Leads.FirstAsync(l => l.LeadCode == "LEAD-00001");

            // 5. Seed Opportunities if empty
            if (!await context.Opportunities.AnyAsync())
            {
                var opps = new List<Opportunity>
                {
                    new Opportunity
                    {
                        OpportunityCode = "OPP-00001",
                        OpportunityName = "Acme Annual Cloud Expansion",
                        CustomerId = c1.CustomerId,
                        Amount = 120000.00m,
                        Stage = OpportunityStage.Proposal,
                        Probability = 50,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                        Status = OpportunityStatus.Open,
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedToId = sales1User.Id,
                        Notes = "Submitted multi-year license proposal."
                    },
                    new Opportunity
                    {
                        OpportunityCode = "OPP-00002",
                        OpportunityName = "Global Logistics Fleet Integration",
                        CustomerId = c2.CustomerId,
                        Amount = 85000.00m,
                        Stage = OpportunityStage.Negotiation,
                        Probability = 75,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(14),
                        Status = OpportunityStatus.Open,
                        CreatedDate = DateTime.UtcNow.AddDays(-35),
                        AssignedToId = sales2User.Id,
                        Notes = "Final contract review with legal department."
                    },
                    new Opportunity
                    {
                        OpportunityCode = "OPP-00003",
                        OpportunityName = "Apex FinTech Core CRM Setup",
                        CustomerId = c3.CustomerId,
                        LeadId = l1.LeadId,
                        Amount = 150000.00m,
                        Stage = OpportunityStage.Won,
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-10),
                        Status = OpportunityStatus.Won,
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        AssignedToId = sales1User.Id,
                        Notes = "Contract signed and onboarded."
                    },
                    new Opportunity
                    {
                        OpportunityCode = "OPP-00004",
                        OpportunityName = "Nexus Health Patient Portal Addon",
                        CustomerId = c4.CustomerId,
                        Amount = 60000.00m,
                        Stage = OpportunityStage.Qualification,
                        Probability = 20,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                        Status = OpportunityStatus.Open,
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        AssignedToId = sales2User.Id,
                        Notes = "Scoping requirements with clinical team."
                    },
                    new Opportunity
                    {
                        OpportunityCode = "OPP-00005",
                        OpportunityName = "Acme Legacy Hardware Upgrade",
                        CustomerId = c1.CustomerId,
                        Amount = 30000.00m,
                        Stage = OpportunityStage.Lost,
                        Probability = 0,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-20),
                        Status = OpportunityStatus.Lost,
                        CreatedDate = DateTime.UtcNow.AddMonths(-3),
                        AssignedToId = sales1User.Id,
                        Notes = "Client delayed hardware refresh budget."
                    }
                };

                context.Opportunities.AddRange(opps);
                await context.SaveChangesAsync();
            }

            var o1 = await context.Opportunities.FirstAsync(o => o.OpportunityCode == "OPP-00001");
            var o2 = await context.Opportunities.FirstAsync(o => o.OpportunityCode == "OPP-00002");

            // 6. Seed Follow-Ups if empty
            if (!await context.FollowUps.AnyAsync())
            {
                var followUps = new List<FollowUp>
                {
                    new FollowUp
                    {
                        RelatedType = FollowUpRelatedType.Opportunity,
                        OpportunityId = o1.OpportunityId,
                        CustomerId = c1.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(2),
                        FollowUpType = FollowUpType.Call,
                        Subject = "Review Proposal Details with Acme VP",
                        Remarks = "Discuss SLA clauses and volume discounts.",
                        Status = FollowUpStatus.Planned,
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        AssignedToId = sales1User.Id
                    },
                    new FollowUp
                    {
                        RelatedType = FollowUpRelatedType.Opportunity,
                        OpportunityId = o2.OpportunityId,
                        CustomerId = c2.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(5),
                        FollowUpType = FollowUpType.Meeting,
                        Subject = "Final Negotiation Roundtable",
                        Remarks = "Present executive summary and contract sign-off.",
                        Status = FollowUpStatus.Planned,
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        AssignedToId = sales2User.Id
                    },
                    new FollowUp
                    {
                        RelatedType = FollowUpRelatedType.Lead,
                        LeadId = l1.LeadId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-1),
                        FollowUpType = FollowUpType.Email,
                        Subject = "Send Product Architecture Whitepaper",
                        Remarks = "Overdue task from yesterday.",
                        Status = FollowUpStatus.Planned,
                        CreatedDate = DateTime.UtcNow.AddDays(-4),
                        AssignedToId = sales1User.Id
                    },
                    new FollowUp
                    {
                        RelatedType = FollowUpRelatedType.Customer,
                        CustomerId = c3.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-7),
                        FollowUpType = FollowUpType.Call,
                        Subject = "Post-Implementation Check-in",
                        Remarks = "Client verified smooth rollout.",
                        Status = FollowUpStatus.Completed,
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedToId = sales1User.Id
                    }
                };

                context.FollowUps.AddRange(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities if empty
            if (!await context.Activities.AnyAsync())
            {
                var activities = new List<Activity>
                {
                    new Activity
                    {
                        ActivityType = ActivityType.Meeting,
                        Subject = "Discovery Workshop with Acme Engineering",
                        Description = "Mapped data pipelines and security compliance.",
                        ActivityDate = DateTime.UtcNow.AddDays(-12),
                        CustomerId = c1.CustomerId,
                        AssignedToId = sales1User.Id,
                        Status = ActivityStatus.Completed,
                        CreatedDate = DateTime.UtcNow.AddDays(-12)
                    },
                    new Activity
                    {
                        ActivityType = ActivityType.Call,
                        Subject = "Quarterly Business Review Prep",
                        Description = "Aligned deliverables with customer leadership.",
                        ActivityDate = DateTime.UtcNow.AddDays(-6),
                        CustomerId = c2.CustomerId,
                        AssignedToId = sales2User.Id,
                        Status = ActivityStatus.Completed,
                        CreatedDate = DateTime.UtcNow.AddDays(-6)
                    },
                    new Activity
                    {
                        ActivityType = ActivityType.Task,
                        Subject = "Draft Custom SLA Appendix",
                        Description = "Prepare custom SLA annexure for Global Logistics.",
                        ActivityDate = DateTime.UtcNow.AddDays(3),
                        CustomerId = c2.CustomerId,
                        AssignedToId = sales2User.Id,
                        Status = ActivityStatus.Planned,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    }
                };

                context.Activities.AddRange(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Audit Logs if empty
            if (!await context.AuditLogs.AnyAsync())
            {
                var logs = new List<AuditLog>
                {
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.UserName,
                        Action = "SystemInit",
                        Module = "Security",
                        EntityName = "System",
                        Result = "Success",
                        IpAddress = "127.0.0.1",
                        CreatedDate = DateTime.UtcNow.AddMonths(-6),
                        Details = "AcxiomCRM initial system startup and role baseline configured."
                    },
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.UserName,
                        Action = "Login",
                        Module = "Auth",
                        EntityName = "ApplicationUser",
                        RecordId = adminUser.Id,
                        Result = "Success",
                        IpAddress = "127.0.0.1",
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        Details = "Administrator successfully authenticated."
                    }
                };

                context.AuditLogs.AddRange(logs);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
