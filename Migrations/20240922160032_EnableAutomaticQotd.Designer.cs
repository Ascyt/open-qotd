﻿// <auto-generated />
using System;
using CustomQotd.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CustomQotd.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240922160032_EnableAutomaticQotd")]
    partial class EnableAutomaticQotd
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("CustomQotd.Database.Entities.Config", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("AdminRoleId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("BasicRoleId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("CurrentSuggestStreak")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("CurrentSuggestStreakUserId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("EnableAutomaticQotd")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("EnableQotdPinMessage")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("EnableQotdUnavailableMessage")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("EnableSuggestions")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("LastQotdMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LastSentDay")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("LogsChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("QotdChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("QotdPingRoleId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("QotdTimeHourUtc")
                        .HasColumnType("INTEGER");

                    b.Property<int>("QotdTimeMinuteUtc")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("SuggestionsChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("SuggestionsPingRoleId")
                        .HasColumnType("INTEGER");

                    b.HasKey("GuildId");

                    b.ToTable("Configs");
                });

            modelBuilder.Entity("CustomQotd.Database.Entities.Question", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("AcceptedByUserId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("AcceptedTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<int>("GuildDependentId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("SentNumber")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("SentTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("SubmittedByUserId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("SuggestionMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Questions");
                });
#pragma warning restore 612, 618
        }
    }
}
