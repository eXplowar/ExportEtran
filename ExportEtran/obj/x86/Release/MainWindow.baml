   M S B A M L   `   `   `  ���� a  ]PresentationFramework.Aero, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35gJclr-namespace:Microsoft.Windows.Themes;assembly=PresentationFramework.AeroMicrosoft.Windows.Themes  F BExportEtran, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:ExportEtranExportEtran )clr-namespace:ExportEtranExportEtran    ExportEtran.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     oMicrosoft_Windows_ThemesJclr-namespace:Microsoft.Windows.Themes;assembly=PresentationFramework.Aero     @t2http://schemas.microsoft.com/expression/blend/2008     +localclr-namespace:ExportEtran     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     (myclr-namespace:ExportEtran     -   $��
mainWindow��  � Title$  ETRAN��$	��587��$
��1603�� � Icon$+ %/ExportEtran;component/doc_export.ico�� � 	Resources    tbl_ConfRefViewSource  ExportEtran.MyList 
 MyList  ExportEtran.SrcFieldList  SrcFieldList  ExportEtran.DstTableList  DstTableList  ExportEtran.CostDic  CostDic  ExportEtran.TestFieldList  TestFieldList��%Z   &	        &	      &	 
     &	      &	      &	      ��           .���  � DataContext# [  .+�o�  o� 
IsMainMenu . .@�m� $��	_Файл��.@�m� -   $��MenuItemExit��$��_Выход��m� $��_Правка��$y�	Collapsed=�m� $��_Вид��$y�	Collapsed=�m� $��_Сервис��.@�m� -   $��MenuItemOpenSettings��$��_Настройки��m� $��	_Окно��$y�	Collapsed=�m� $��_Справка��.@�m� -   $��MenuItemAbout��$��_О программе���� -   $��tabControlMain��$��	0,23,0,23q�.@��� -   $��tabItemImportDoc��$9��3Импорт документов из ЭТРАНа��.��� .+��� -   $��tabControl1��$��6,35,6,6q�.@��� -   $��tabBill��$��ЖД накладная��.��� .+��� -	   $��btnExportBill��$5��/Экспортировать накладные��$��23��$��6,6,0,0q�$	��Top=�$
��Left=�$	��160���� -
   $��textBlockCarNumLog��$
��Auto��$
��Left=�$��6,35,0,0q�$��Лог: ��$	��Top=�$
��1290��$��CharacterEllipsis=�$
��Wrap=�$��43��� $��0,57,0,0q� � RowDefinitions �� $
��141* ��� $
��129* �.+��� -   $��textBlockBill��$��18��$
��Left=�$��6,6,0,0q�$��	tbl_Bill:��$	��Top=�&   System.Windows.Controls.DataGrid  -   $��dataGridBillLog��   AlternatingRowBackground �	#01FFFFFF   AutoGenerateColumns . $��6,30,6,0q� �� Left$ 0��
	 �� Top$	 0���� -   $��gridSplitterBill��
 �� ResizeDirection$

 Rows=�$��5��$	��Top=�$��1��$��Stretch=��� -   $��textBlockBillCar��$��18��$
��Left=�$��6,6,0,0q�$��tbl_CarNum:��$	��Top=�$��1��  -   $��dataGridBillCarLog�� �	#01FFFFFF . $��6,30,6,6q�$��1��$ 0��$	 0���� -   $��tabVPU��$��ВПУ��.��� .+��� -   $��btnExportVPU��$)��#Экспортировать ВПУ��$��23��$��6,6,0,0q�$	��Top=�$
��Left=�$	��160���� -   $��textBlockVpuLog��$
��Auto��$
��Left=�$��6,35,0,0q�$��Лог: ��$	��Top=�$
��1290��$��CharacterEllipsis=�$
��Wrap=�$��43��� $��0,57,0,0q� �� $
��141* ��� $
��129* �.+��� -   $��textBlockRgd��$��18��$
��Left=�$��6,6,0,0q�$��tbl_DocPU_RGD:��$	��Top=�  -   $��dataGridRgdLog�� �	#01FFFFFF . $��6,30,6,0q�$ 0��$	 0���� -   $��gridSplitterRgd��$

 Rows=�$��5��$	��Top=�$��1��$��Stretch=��� -   $��textBlockRgdExpense��$��18��$
��Left=�$��6,6,0,0q�$��tbl_DocPU_RGDExpense:��$	��Top=�$��1��  -   $��dataGridRgdExpenseLog�� �	#01FFFFFF . $��6,30,6,6q�$��1��$ 0��$	 0���� -   $��tabNK��$1��+Накопительная карточка��.��� .+��� -   $��btnExportNK��$'��!Экспортировать НК��$��23��$��6,6,0,0q�$	��Top=�$
��Left=�$	��160���� -   $��textBlockNkLog��$
��Auto��$
��Left=�$��6,35,0,0q�$��Лог: ��$	��Top=�$
��1290��$��CharacterEllipsis=�$
��Wrap=�$��43��  -   $��dataGridNkLog�� . $��6,69,6,6q� �	#01FFFFFF�� -   $
��tabZ��$��Заявка��.��� .+��� -   $��
btnExportZ��$/��)Экспортировать заявки��$��23��$��6,6,0,0q�$	��Top=�$
��Left=�$	��160���� -   $��textBlockZayavkaLog��$
��Auto��$
��Left=�$��6,35,0,0q�$��Лог: ��$	��Top=�$
��1290��$��CharacterEllipsis=�$
��Wrap=�$��43��  -   $��dataGridZayavkaLog�� . $��6,69,6,6q� �	#01FFFFFF�� -    $��tabItemDislocationExcel��$4��.Дислокация вагонов из Excel��.��� .+��� -!   $��btnExportDislocationExcel��$Q��KЭкспортировать дислокацию вагонов из Excel��$��23��$��6,6,0,0q�$	��Top=�$
��Left=�$	��272���� -"   $"��textBlockDislocationExcelLog��$
��Auto��$
��Left=�$��6,35,0,0q�$��Лог: ��$	��Top=�$
��1290��$��CharacterEllipsis=�$
��Wrap=�$��43��  -#   $!��dataGridDislocationExcelLog�� . $��6,69,6,6q� �	#01FFFFFF�� -$   $��txtSourceFile��$��23��$
��Left=�$��11,6,0,0q�$	��Top=�$	��504���� -%   $��btnOpenSourceFile��$��Открыть��$��23��$
��Left=�$��	521,5,0,0q�$	��Top=�$��75���� -&   $��tabItemSettings��$'��!Настройки импорта��.��� .+��� -'   $��tabControlSettings��$��6,0,6,6q�.@��� -(   $��tabEtranSettings��$9��3Импорт документов из ЭТРАНа��.��� .+��� -)   $��textBlockSrcTable��$��23��$
��Left=�$��6,6,0,0q�$4��.Список таблиц источников��$	��Top=�$	��154��  -*   $��tbl_ConfRefDataGrid�� .  $��6,32,6,6q�   HorizontalGridLinesBrush �	#3C000000   VerticalGridLinesBrush �	#3C000000$ 0��$	 0��   MinRowHeight$ 25Q� �	#01FFFFFF   EnableRowVirtualization .  ����    Columns 4  .System.Windows.Controls.DataGridTemplateColumn  -+   srcTableColumn��,  &System.Windows.Controls.DataGridColumn   Header$% Таблица источник��   Width5	  /System.Windows.Controls.DataGridLengthConverter$
 Auto	    CellTemplate �� .R��� $��Center=����� *
SrcTable+   CellEditingTemplate �� .R���  ��Name$ cboSrcTable��#��[  �� 
IsEditable .  �� SelectedItem �� *
SrcTable+ �� Text �� *
SrcTable+  -,   srcFieldColumn��$ Поле источник��$
 Auto	  �� .R��� $��Center=����� *
SrcField+ �� .R��� $ cboSrcField�� .  �� 
IsReadOnly . ����  �� RelativeSource �� *FindAncestor+ �� AncestorType$ WindowL� �� AncestorLevel$ 1�� �� Path$ 	SrcFields� �� *
SrcField+ �� *
SrcField+  --   dstTableColumn��$% Таблица приемник��$
 Auto	  �� .R��� $��Center=����� *
DstTable+ �� .R��� $ cboDstTable��#��[  .  .  �� *
DstTable+ �� *
DstTable+  -.   dstFieldColumn��$ Поле приемник��$
 Auto	  �� .R��� $��Center=����� *
DstField+ �� .R��� $ cboDstField�� .  . ����  �� *FindAncestor+$ WindowL�$ 1��$ 
DestFields� �� *
DstField+ �� *
DstField+  -/   parentTableColumn��$- 'Родительская таблица��$
 Auto	  �� .R��� $��Center=����� *ParentTable+ �� .R��� $ cboParentTable��#��[  .  �� *ParentTable+ �� *ParentTable+4
  .System.Windows.Controls.DataGridCheckBoxColumn
  -0   idFieldColumn��$ Ключ��$ SizeToHeader	 1  +System.Windows.Controls.DataGridBoundColumn   Binding �� $ IdField�   ElementStyle ��  �� 
TargetType" ��.���   �� Property  � ��! �� Value$! Center=���   � ��$! Center=�
  -1   updateFieldColumn��$ Обновлять��$ SizeToHeader	  �� $ UpdateField� �� " ��.���   � ��$! Center=���   � ��$! Center=�0  *System.Windows.Controls.DataGridTextColumn  $ Актуальность��$ SizeToHeader	  �� $ ActualField� �� " ��.���   � ��$! Center=���   � ��$! Center=�  -2   costTableColumn��$ Вид затрат��$
 Auto	 "   MinWidth$	" 200Q� �� .R��� $ cboCostName��-3   # �� DisplayMemberPath$# Value��$ �� SelectedValuePath$	$ Key��  ComboBoxTransparentStyle#��[ $��23�� . 
������ $��0q����� % �� Source#% [ & �� SelectedValue& �� $ CostName2_ID�  $	" 100Q�'   
Visibility$' Hidden=� �� $ CostName2_ID�  $	" 100Q�$' Hidden=� �� $ DstField�  -4   	tstColumn��$ 
Test Field��$
 Auto	 $' Hidden=� �� .R��� $ cboTestColumn�� .  . ���� (   Instance#% Z( $ MyList2� �� *
DstField+ �� *
DstField+�� -5   $��cboSrcTableList��$��23��$
��Left=�$��	167,6,0,0q�$	��Top=�$	��229��) �� SelectedIndex$) 0���� -6   $��btnCheckConfTable��$Z��TПроверить конфигурационную таблицу на ошибки��$��23��$
��Left=�$��	402,6,0,0q�$	��Top=�$	��292���� -7   $��textBox1��$��23��$��1514,0,0,444q�$��Bottom=�$
��Left=�$��48��$y�Hidden=����� * �� ElementName$* 
mainWindow��$ TextData��� -8   $��button1��$=��7Изменить источник строк (DstField)��$��23��$
��Left=�$��Bottom=�$	��205��$y�Hidden=�$��1032,0,0,445q��� -9   $��button2��$K��EДобавить в выподающий список (TestFieldList)��$��23��$
��Left=�$��1243,0,0,445q�$��Bottom=�$	��265��$y�Hidden=��� -:   $!��tabDislocationExcelSettings��$A��;Импорт дислокаций вагонов из Excel��.��� �� -;   $��
statusBar1��$��23��$��Bottom=����� .R�� + � ColumnDefinitions+ �� $��* ��� $
��Auto �.@��� $��0��.���� -<   $��StatusBarItemLeft���� $��1��$��0��$��Right=�$	��0,4q��� $��1��.���� -=   $��StatusBarItemRight��
Время