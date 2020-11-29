﻿// <auto-generated />
using System;
using DigitRecognitionLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DigitRecognitionLibrary.Migrations
{
    [DbContext(typeof(LibraryContext))]
    [Migration("20201126211410_init")]
    partial class init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("DigitRecognitionLibrary.Blob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Image")
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.ToTable("ImageDetails");
                });

            modelBuilder.Entity("DigitRecognitionLibrary.ImageObj", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<float>("Confidence")
                        .HasColumnType("REAL");

                    b.Property<int?>("ImageDetailsId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LabelObjectId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ImageDetailsId");

                    b.HasIndex("LabelObjectId");

                    b.ToTable("ImageObjs");
                });

            modelBuilder.Entity("DigitRecognitionLibrary.LabelObj", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Label")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StatCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("LabelObjs");
                });

            modelBuilder.Entity("DigitRecognitionLibrary.ImageObj", b =>
                {
                    b.HasOne("DigitRecognitionLibrary.Blob", "ImageDetails")
                        .WithMany()
                        .HasForeignKey("ImageDetailsId");

                    b.HasOne("DigitRecognitionLibrary.LabelObj", "LabelObject")
                        .WithMany("ImageObjs")
                        .HasForeignKey("LabelObjectId");

                    b.Navigation("ImageDetails");

                    b.Navigation("LabelObject");
                });

            modelBuilder.Entity("DigitRecognitionLibrary.LabelObj", b =>
                {
                    b.Navigation("ImageObjs");
                });
#pragma warning restore 612, 618
        }
    }
}
