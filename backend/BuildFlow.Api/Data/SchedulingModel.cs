using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Data;

public static class SchedulingModel
{
    public static void Configure(ModelBuilder model)
    {
        foreach (var type in new[] { typeof(Worker), typeof(Skill), typeof(WorkerSkill), typeof(Shift), typeof(Equipment), typeof(WorkSchedule), typeof(WorkerAssignment), typeof(EquipmentReservation), typeof(EquipmentRequest), typeof(SiteIssue) })
        {
            model.Entity(type).Property(nameof(SchedulingRecord.Name)).HasMaxLength(160).IsRequired();
            model.Entity(type).Property(nameof(SchedulingRecord.Notes)).HasMaxLength(2000);
        }
        model.Entity<Worker>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkerSkill>().HasOne<Worker>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkerSkill>().HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkerSkill>().HasIndex(x => new { x.WorkerId, x.SkillId }).IsUnique().HasFilter("\"IsArchived\" = false");
        model.Entity<Shift>().HasOne<Worker>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Equipment>().HasIndex(x => x.Code).IsUnique();
        model.Entity<Equipment>().Property(x => x.Code).HasMaxLength(40);
        model.Entity<Equipment>().Property(x => x.Category).HasMaxLength(80);
        model.Entity<WorkSchedule>().ToTable("WorkSchedules");
        model.Entity<WorkSchedule>().HasOne<ConstructionActivity>().WithMany().HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkSchedule>().HasOne<WorkSchedule>().WithMany().HasForeignKey(x => x.DependencyId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkSchedule>().HasOne<PlanningWorkflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkSchedule>().HasIndex(x => x.WorkflowId).IsUnique();
        model.Entity<WorkerAssignment>().HasOne<Worker>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkerAssignment>().HasOne<Skill>().WithMany().HasForeignKey(x => x.RequiredSkillId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkerAssignment>().HasOne<WorkSchedule>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<EquipmentReservation>().HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<EquipmentReservation>().HasOne<WorkSchedule>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<EquipmentRequest>().HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<EquipmentRequest>().HasOne<ConstructionActivity>().WithMany().HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SiteIssue>().HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SiteIssue>().HasOne<ConstructionActivity>().WithMany().HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Restrict);
    }
}
