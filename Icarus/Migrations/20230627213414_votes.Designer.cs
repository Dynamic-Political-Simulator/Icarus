﻿// <auto-generated />
using System;
using Icarus.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Icarus.Migrations
{
    [DbContext(typeof(IcarusContext))]
    [Migration("20230627213414_votes")]
    partial class votes
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Icarus.Context.Models.CharacterToken", b =>
                {
                    b.Property<string>("PlayerCharacterId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Amount")
                        .HasColumnType("int");

                    b.Property<string>("TokenTypeId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("PlayerCharacterId");

                    b.HasIndex("TokenTypeId");

                    b.ToTable("Tokens");
                });

            modelBuilder.Entity("Icarus.Context.Models.CharacterTokenType", b =>
                {
                    b.Property<string>("TokenTypeName")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("TokenTypeName");

                    b.ToTable("TokenTypes");
                });

            modelBuilder.Entity("Icarus.Context.Models.DeathTimer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CharacterId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("TimeKilled")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("DeathTimer");
                });

            modelBuilder.Entity("Icarus.Context.Models.DebugChannel", b =>
                {
                    b.Property<decimal>("ChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("ChannelId");

                    b.ToTable("DebugChannels");
                });

            modelBuilder.Entity("Icarus.Context.Models.DiscordUser", b =>
                {
                    b.Property<string>("DiscordId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("CanUseAdminCommands")
                        .HasColumnType("bit");

                    b.HasKey("DiscordId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Icarus.Context.Models.GameState", b =>
                {
                    b.Property<int>("GameStateId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("GameStateId"));

                    b.Property<bool>("AgingEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastAgingEvent")
                        .HasColumnType("datetime2");

                    b.Property<long>("LastTickEpoch")
                        .HasColumnType("bigint");

                    b.Property<int?>("NationId")
                        .HasColumnType("int");

                    b.Property<long>("TickInterval")
                        .HasColumnType("bigint");

                    b.Property<int>("Year")
                        .HasColumnType("int");

                    b.HasKey("GameStateId");

                    b.HasIndex("NationId");

                    b.ToTable("GameStates");

                    b.HasData(
                        new
                        {
                            GameStateId = 1,
                            AgingEnabled = false,
                            LastAgingEvent = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            LastTickEpoch = 0L,
                            TickInterval = 3600000L,
                            Year = 0
                        });
                });

            modelBuilder.Entity("Icarus.Context.Models.Good", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TAG")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("WealthMod")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.ToTable("Goods");
                });

            modelBuilder.Entity("Icarus.Context.Models.GoodValueModifier", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<float>("Decay")
                        .HasColumnType("real");

                    b.Property<int>("GoodWrapperId")
                        .HasColumnType("int");

                    b.Property<float>("Modifier")
                        .HasColumnType("real");

                    b.Property<string>("ValueTag")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.HasIndex("GoodWrapperId");

                    b.ToTable("GoodValueModifiers");
                });

            modelBuilder.Entity("Icarus.Context.Models.GraveyardChannel", b =>
                {
                    b.Property<decimal>("ChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("ChannelId");

                    b.ToTable("GraveyardChannels");
                });

            modelBuilder.Entity("Icarus.Context.Models.GroupOfInterest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("GroupOfInterests");
                });

            modelBuilder.Entity("Icarus.Context.Models.Modifier", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("NationId")
                        .HasColumnType("int");

                    b.Property<int?>("ProvinceId")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<float>("WealthMod")
                        .HasColumnType("real");

                    b.Property<bool>("isGood")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("NationId");

                    b.HasIndex("ProvinceId");

                    b.ToTable("Modifiers");
                });

            modelBuilder.Entity("Icarus.Context.Models.Nation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Nations");
                });

            modelBuilder.Entity("Icarus.Context.Models.PlayerCharacter", b =>
                {
                    b.Property<string>("CharacterId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Career")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CharacterDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CharacterName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Culture")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DiscordUserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int?>("GoIid")
                        .HasColumnType("int");

                    b.Property<string>("PrivilegedGroup")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("YearOfBirth")
                        .HasColumnType("int");

                    b.Property<int>("YearOfDeath")
                        .HasColumnType("int");

                    b.HasKey("CharacterId");

                    b.HasIndex("DiscordUserId");

                    b.HasIndex("GoIid");

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("Icarus.Context.Models.Province", b =>
                {
                    b.Property<int>("ProvinceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ProvinceId"));

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NationId")
                        .HasColumnType("int");

                    b.HasKey("ProvinceId");

                    b.HasIndex("NationId");

                    b.ToTable("Provinces");
                });

            modelBuilder.Entity("Icarus.Context.Models.Value", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<float>("BaseBalue")
                        .HasColumnType("real");

                    b.Property<float>("CurrentValue")
                        .HasColumnType("real");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ProvinceId")
                        .HasColumnType("int");

                    b.Property<string>("TAG")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ProvinceId");

                    b.ToTable("Values");
                });

            modelBuilder.Entity("Icarus.Context.Models.ValueHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<float>("Change")
                        .HasColumnType("real");

                    b.Property<float>("Goal")
                        .HasColumnType("real");

                    b.Property<float>("Height")
                        .HasColumnType("real");

                    b.Property<int>("ValueId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ValueId");

                    b.ToTable("valueHistories");
                });

            modelBuilder.Entity("Icarus.Context.Models.ValueModifier", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<float>("Decay")
                        .HasColumnType("real");

                    b.Property<float>("Modifier")
                        .HasColumnType("real");

                    b.Property<int>("ModifierWrapperId")
                        .HasColumnType("int");

                    b.Property<string>("ValueTag")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.HasIndex("ModifierWrapperId");

                    b.ToTable("ValueModifiers");
                });

            modelBuilder.Entity("Icarus.Context.Models.ValueRelationship", b =>
                {
                    b.Property<int>("ValueRelationShipId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ValueRelationShipId"));

                    b.Property<string>("OriginTag")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TargetTag")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("Weight")
                        .HasColumnType("real");

                    b.HasKey("ValueRelationShipId");

                    b.ToTable("Relationships");
                });

            modelBuilder.Entity("Icarus.Context.Models.VoteMessage", b =>
                {
                    b.Property<decimal>("MessageId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("CreatorId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<long>("EndTime")
                        .HasColumnType("bigint");

                    b.Property<long>("TimeSpan")
                        .HasColumnType("bigint");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("MessageId");

                    b.ToTable("VoteMessages");
                });

            modelBuilder.Entity("Icarus.Context.Models.CharacterToken", b =>
                {
                    b.HasOne("Icarus.Context.Models.PlayerCharacter", "Character")
                        .WithMany("Tokens")
                        .HasForeignKey("PlayerCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Icarus.Context.Models.CharacterTokenType", "TokenType")
                        .WithMany()
                        .HasForeignKey("TokenTypeId");

                    b.Navigation("Character");

                    b.Navigation("TokenType");
                });

            modelBuilder.Entity("Icarus.Context.Models.GameState", b =>
                {
                    b.HasOne("Icarus.Context.Models.Nation", "Nation")
                        .WithMany()
                        .HasForeignKey("NationId");

                    b.Navigation("Nation");
                });

            modelBuilder.Entity("Icarus.Context.Models.GoodValueModifier", b =>
                {
                    b.HasOne("Icarus.Context.Models.Good", "GoodWrapper")
                        .WithMany("ValueModifiers")
                        .HasForeignKey("GoodWrapperId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GoodWrapper");
                });

            modelBuilder.Entity("Icarus.Context.Models.Modifier", b =>
                {
                    b.HasOne("Icarus.Context.Models.Nation", null)
                        .WithMany("Modifiers")
                        .HasForeignKey("NationId");

                    b.HasOne("Icarus.Context.Models.Province", null)
                        .WithMany("Modifiers")
                        .HasForeignKey("ProvinceId");
                });

            modelBuilder.Entity("Icarus.Context.Models.PlayerCharacter", b =>
                {
                    b.HasOne("Icarus.Context.Models.DiscordUser", "DiscordUser")
                        .WithMany("Characters")
                        .HasForeignKey("DiscordUserId");

                    b.HasOne("Icarus.Context.Models.GroupOfInterest", "GroupOfInterest")
                        .WithMany("Characters")
                        .HasForeignKey("GoIid");

                    b.Navigation("DiscordUser");

                    b.Navigation("GroupOfInterest");
                });

            modelBuilder.Entity("Icarus.Context.Models.Province", b =>
                {
                    b.HasOne("Icarus.Context.Models.Nation", "Nation")
                        .WithMany("Provinces")
                        .HasForeignKey("NationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Nation");
                });

            modelBuilder.Entity("Icarus.Context.Models.Value", b =>
                {
                    b.HasOne("Icarus.Context.Models.Province", "Province")
                        .WithMany("Values")
                        .HasForeignKey("ProvinceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Province");
                });

            modelBuilder.Entity("Icarus.Context.Models.ValueHistory", b =>
                {
                    b.HasOne("Icarus.Context.Models.Value", "Value")
                        .WithMany("PastValues")
                        .HasForeignKey("ValueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Value");
                });

            modelBuilder.Entity("Icarus.Context.Models.ValueModifier", b =>
                {
                    b.HasOne("Icarus.Context.Models.Modifier", "ModifierWrapper")
                        .WithMany("Modifiers")
                        .HasForeignKey("ModifierWrapperId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModifierWrapper");
                });

            modelBuilder.Entity("Icarus.Context.Models.DiscordUser", b =>
                {
                    b.Navigation("Characters");
                });

            modelBuilder.Entity("Icarus.Context.Models.Good", b =>
                {
                    b.Navigation("ValueModifiers");
                });

            modelBuilder.Entity("Icarus.Context.Models.GroupOfInterest", b =>
                {
                    b.Navigation("Characters");
                });

            modelBuilder.Entity("Icarus.Context.Models.Modifier", b =>
                {
                    b.Navigation("Modifiers");
                });

            modelBuilder.Entity("Icarus.Context.Models.Nation", b =>
                {
                    b.Navigation("Modifiers");

                    b.Navigation("Provinces");
                });

            modelBuilder.Entity("Icarus.Context.Models.PlayerCharacter", b =>
                {
                    b.Navigation("Tokens");
                });

            modelBuilder.Entity("Icarus.Context.Models.Province", b =>
                {
                    b.Navigation("Modifiers");

                    b.Navigation("Values");
                });

            modelBuilder.Entity("Icarus.Context.Models.Value", b =>
                {
                    b.Navigation("PastValues");
                });
#pragma warning restore 612, 618
        }
    }
}
