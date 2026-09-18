# TrueText

**OCR-Based Document Digitalization Desktop App**

TrueText is a Windows desktop application designed to digitize printed documents by scanning physical pages and converting their contents into editable digital text.

The application uses **Tesseract OCR** to recognize **Sinhala and English** text, while HOCR parsing is used to preserve the original document layout when exporting the recognized content to Word or PDF.

## Features

* **Sinhala & English OCR** - Extract text from printed documents using Tesseract OCR.
* **Document Scanning** - Scan physical documents directly from supported scanners.
* **Scanner Selection** - Select and configure the connected scanner through WIA.
* **Scan Settings** - Configure scanning options before capturing a document.
* **Image Preview** - Preview scanned pages before processing them.
* **Layout-Preserved Export** - Convert OCR results into Word and PDF documents while retaining the original text positioning.
* **Local Storage** - Store application data locally using SQLite.

## Technology Stack

| Technology    | Purpose                         |
| ------------- | ------------------------------- |
| C#            | Application development         |
| .NET          | Application framework           |
| Avalonia UI   | Desktop user interface          |
| Tesseract OCR | Text recognition                |
| SQLite        | Local data storage              |
| WIA           | Scanner integration             |
| HOCR          | OCR layout and positioning data |



## Supported Languages

Currently supported OCR languages:

* Sinhala
* English

## Requirements

* Windows
* .NET runtime/development environment
* A WIA-compatible scanner for direct scanning
* Tesseract OCR language data for the supported languages

## Purpose

The project explores how traditional paper-based documents can be converted into editable digital documents while retaining useful aspects of their original structure.

It combines desktop application development, OCR, hardware integration, image processing, document generation, and local data management into a single workflow.

