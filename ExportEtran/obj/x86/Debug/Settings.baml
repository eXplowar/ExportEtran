   M S B A M L   `   `   `  ����  ExportEtran    ExportEtran.Settings   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  Настройки��5   	   $	��275��6   $	��468��6(    � Icon$+ %/ExportEtran;component/doc_export.ico��6K    � Topmost . .��5      � .+�5   
   �� -   5        $��cboWcfServerList��5   M   $��23��6   $
��Left=�6   $��	58,12,0,0q�6:   $	��Top=�6e   $	��376��6}   6�   �� 5   
   -   5        $��
textBlock1��5   N   $��23��6   $
��Left=�6    $��	12,12,0,0q�6;   $��Сервер��6`   $	��Top=�6n    �� Padding$ 0,3,0,0q�6�   9� 5   
   $#��Добавить сервер��6   $��	11,41,0,0q�6,   $	��183��6?   $	��Top=�6L   $
��Left=�6d   $	��424��6   .��5	      � -   5        $��gridAddClientServer��5	       � ColumnDefinitions 5
      �� 5      $
��136* �6'   �� 5      $
��257* �6'   .+�5      �� -   5        $��textBlockAddress��5   T   $��23��6   $
��Left=�6(   $��6,6,0,0q�6C   $��Address��6l   $	��Top=�6{   $ 0,3,0,0q�6�    � ToolTip 5      _� 5      .��5      �� .+�5      �� $
��Bold"�6(   Адрес сервера6I   �� 5      2  ,System.Windows.Markup.XmlAttributeProperties  XmlSpace preserve6K   $	��600��6(   $
��Wrap=�67   ��Обязательный строковый атрибут.
Задает адрес конечной точки. Значение по умолчанию — пустая строка. Адрес должен быть абсолютным универсальным кодом ресурса (URI).5   �   5      5      5      �� 5      -   5        $��textBlockBinding��5   U   $��23��6   $
��Left=�6(   $��6,29,0,0q�6C   $��Binding��6m   $	��Top=�6|   $ 0,3,0,0q�6�    5      _� 5      .��5      �� .+�5      �� $
��Bold"�6(   Тип привязки6H   �� 5       preserve6K   $	��600��6(   $
��Wrap=�67   ��Обязательный строковый атрибут.
Строка, указывающая тип привязки для использования. Для того чтобы на тип можно было ссылаться, он должен иметь зарегистрированный раздел конфигурации. Тип регистрируется по имени раздела, а не по имени типа привязки.5   �   5       5!      5#      �� 5$      -   5        $#��textBlockBindingConfiguration��5$   U   $��23��6   $
��Left=�6(   $��6,52,0,0q�6C   $��BindingConfiguration��6z   $	��Top=�6�   $ 0,3,0,0q�6�    5%      _� 5&      .��5'      �� .+�5(      �� $
��Bold"�6(   20Имя конфигурации привязки6U   �� 5)       preserve6K   $	��600��6(   $
��Wrap=�67   ��Необязательный. Строка, содержащая имя конфигурации привязки для использования при создании экземпляра конечной точки. Конфигурация привязки должна входить в область действия в точке определения конечной точки. Значение по умолчанию — пустая строка.
Этот атрибут используется вместе с атрибутом binding для ссылки на конкретную конфигурацию привязки в файле конфигурации. Задайте этот атрибут, если выполняется попытка использовать пользовательскую привязку. В противном случае может быть создано исключение.5*     5+      5,      5.      �� 5/      -   5        $��textBlockContract��5/   U   $��23��6   $
��Left=�6(   $��6,75,0,0q�6C   $��Contract��6n   $	��Top=�6~   $ 0,3,0,0q�6�    50      _� 51      .��52      �� .+�53      �� $
��Bold"�6(   Контракт6D   �� 54       preserve6K   $	��600��6(   $
��Wrap=�67   ��Обязательный строковый атрибут.
Строка, указывающая, к какому контракту предоставляется доступ этой конечной точкой. В сборке должен быть реализован данный тип контракта.55   �   56      57      59      �� 5:      -	   5        $��textBlockName��5:   U   $��23��6   $
��Left=�6(   $��6,98,0,0q�6C   $
��Name��6j   $	��Top=�6v   $ 0,3,0,0q�6�    5;      _� 5<      .��5=      �� .+�5>      �� $
��Bold"�6(   $"Имя конечной точки6N   �� 5?       preserve6K   $	��600��6(   $
��Wrap=�67   ��Необязательный строковый атрибут. Этот атрибут уникальным образом идентифицирует конечную точку для данного контракта. Для данного типа контракта можно определить несколько клиентов. Определения должны отличаться друг от друга уникальным именем конфигурации. Если этот атрибут опущен, соответствующая конечная точка используется как конечная точка по умолчанию, связанная с заданным типом контракта. Значение по умолчанию — пустая строка.
Атрибут name привязки используется для экспорта определения посредством языка WSDL.5@   V   5A      5B      5D      �� 5E      -
   5        $��textBoxAddress��5E   R   $��23��6   $
��Left=�6&   $��0,6,0,0q�6A   $	��Top=�6h   $	��260��6�   $��1��6�   $/��)http://localhost:22222/ParsingService.svc��6�    6�   ��  �� ElementName$ textBlockAddress�� �� Path$ ToolTip��� 5F      -   5        $��textBoxBinding��5F   S   $��23��6   $
��Left=�6&   $��0,29,0,0q�6A   $	��Top=�6i   $	��260��6�   $��1��6�   $��basicHttpBinding��6�    6�   �� $ textBlockBinding��$ ToolTip��� 5G      -   5        $!��textBoxBindingConfiguration��5G   S   $��23��6   $
��Left=�6&   $��0,52,0,0q�6A   $	��Top=�6v   $	��260��6�   $��1��6�   $&�� BasicHttpBinding_IParsingService��6�    6�   �� $# textBlockBindingConfiguration��$ ToolTip��� 5H      -   5        $��textBoxContract��5H   S   $��23��6   $
��Left=�6&   $��0,75,0,0q�6A   $	��Top=�6j   $	��260��6�   $��1��6�   $2��,EtranParsingServiceReference.IParsingService��6�    6�   �� $ textBlockContract��$ ToolTip��� 5I      -   5        $��textBoxName��5I   S   $��23��6   $
��Left=�6&   $��0,98,0,0q�6A   $	��Top=�6f   $	��260��6~   $��1��6�   $��	MyBinding��6�    6�   �� $ textBlockName��$ ToolTip��� 5J      -   5        $��btnAddEndpoint��5J   M   $��Добавить��6   $��23��6,   $��185,127,0,0q�68   $	��Top=�6c   $��1��6�   $
��Left=�6�   $��75��6�   5K      5L      5M      5N      