#include "calculate.h"
/*
	��˾����ŵ�Ƽ�
	���ߣ��
	ʱ�䣺2019.6
	�Ա���https://shop341500796.taobao.com/?spm=a230r.7195193.1997079397.2.628a4fb6DAI9Pc
*/
//ʹ��ʱֻ���calculatestruct�ṹ����ȡ����Ӧ��ֵ���ɡ�
calculate_typedef calculatestruct;

uint8_t rxcnt =0;
uint8_t i=0;
void GetJoybuff(uint8_t data)
{
	uint8_t rxbuff[50];
  rxbuff[rxcnt++] = data;
	if(rxbuff[0]!=0xAA){rxcnt = 0;return;}
	if(rxcnt<sizeof(calculatestruct.rx_data)){return;}
	for(i=0;i<sizeof(calculatestruct.rx_data);i++)
	{
	calculatestruct.byte[i] = rxbuff[i];
	}
	rxcnt =0;	
}



