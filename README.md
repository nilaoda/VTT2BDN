# VTT2BDN
Convert vtt with png to BDN format (xml+png)

# Source
```
WEBVTT

00:52.428 --> 00:56.682 line:100%,end position:50%,center
0001.png

02:05.417 --> 02:06.836 line:100%,end position:50%,center
0002.png

02:12.883 --> 02:14.760 line:100%,end position:50%,center
0003.png

02:14.844 --> 02:18.597 line:100%,end position:50%,center
0004.png
```

# Output
```xml
<?xml version="1.0" encoding="utf-8"?>
<BDN Version="0.93" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="BD-03-006-0093b BDN File Format.xsd">
  <Description>
    <Name Title="subtitle_from_vtt" Content="" />
    <Language Code="eng" />
    <Format VideoFormat="1920x1080" FrameRate="23.976" DropFrame="False" />
    <Events Type="Graphic" FirstEventInTC="00:00:52:10" LastEventOutTC="00:02:18:14" NumberofEvents="4" />
  </Description>
  <Events>
    <Event InTC="00:00:52:10" OutTC="00:00:56:16" Forced="false">
      <Graphic Width="75" Height="41" X="922" Y="1019">0001.png</Graphic>
    </Event>
    <Event InTC="00:02:05:10" OutTC="00:02:06:20" Forced="false">
      <Graphic Width="189" Height="41" X="865" Y="1019">0002.png</Graphic>
    </Event>
    <Event InTC="00:02:12:21" OutTC="00:02:14:18" Forced="false">
      <Graphic Width="237" Height="41" X="841" Y="1019">0003.png</Graphic>
    </Event>
    <Event InTC="00:02:14:20" OutTC="00:02:18:14" Forced="false">
      <Graphic Width="306" Height="73" X="807" Y="987">0004.png</Graphic>
    </Event>
  </Events>
</BDN>
```
