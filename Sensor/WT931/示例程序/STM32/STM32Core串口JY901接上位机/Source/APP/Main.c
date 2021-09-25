/*
��д�ߣ�Kevin
��ַ��http://RobotControl.taobao.com
����E-mail��1609370741@qq.com
���뻷����MDK-Lite  Version: 5.17
����ʱ��: 2016-1-31
���ԣ� ���������ڡ������ǿء���STM32Coreƽ̨����ɲ���
���ܣ�
��STM32Coreƽ̨����2��ȡJY901�����ݣ�Ȼ��ͨ������1ֱ�ӽӵ���λ������λ����ѡ������9600��
�ô��ڵ��������������16��������
����
USB-TTL����                 STM32Core              JY901
VCC          -----           VCC        ----        VCC
TX           -----           RX1  ���ܽ�10��   
RX           -----           TX1   (�ܽ�9)
GND          -----           GND    ----        GND
                             RX2     ���ܽ�3��  ----        TX
							 TX2     ���ܽ�2�� ----        RX
------------------------------------
 */
#include <string.h>
#include <stdio.h>
#include "Main.h"
#include "REG.h"
#include "stm32f10x_rcc.h"
#include "stm32f10x_gpio.h"
#include "UART1.h"
#include "UART2.h"
#include "delay.h"
#include "IOI2C.h"
#include "hw_config.h"
#include "DIO.h"
void CopeSerial1Data(unsigned char ucData)
{	
	UART2_Put_Char(ucData);
}
void CopeSerial2Data(unsigned char ucData)
{
	LED_REVERSE();
	UART1_Put_Char(ucData);
	USB_TxWrite(&ucData,1);
}
int main(void)
{  
	unsigned char str[100];
	unsigned char len,i;
		
	USB_Config();		
	SysTick_init(72,10);
	Initial_UART1(9600);//��PC�Ĵ���
	Initial_UART2(9600);//��JY-901ģ��Ĵ���	
	
	LED_ON();
	while (1)
	{
		delay_ms(1);
		len = USB_RxRead(str, sizeof(str));
		for (i=0;i<len;i++)
		{
				UART2_Put_Char(str[i]);
		}
	}
}



