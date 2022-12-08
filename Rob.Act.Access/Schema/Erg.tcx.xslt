<?xml version="1.0" encoding="utf-8"?>
<x:stylesheet version="1.0" xmlns:x="http://www.w3.org/1999/XSL/Transform" xmlns:m="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="m tcx #default"
			  xmlns:tcx="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2" xmlns="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"
			  >
  <x:output method="xml" indent="yes" omit-xml-declaration="yes"/>
  
  <x:template match="@*|node()">
	<x:copy>
	  <x:apply-templates select="@*|node()"/>
	</x:copy>
  </x:template>

  <x:template match="tcx:Track"/>
  <x:template match="tcx:Creator"/>
  <x:template match="tcx:Author"/>

  <x:template match="tcx:Id">
	<x:copy>
	  <x:apply-templates select="@*|node()"/>
	</x:copy>
	<Drag>135</Drag>
	<Action>O↑</Action>
	<Subject>Rob</Subject>
	<Locus>Home☯</Locus>
	<Refine>[4-2]′×4</Refine>
  </x:template>

</x:stylesheet>
