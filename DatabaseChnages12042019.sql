Create Procedure [dbo].[GetEmployeeSessionDetail]
@ContraryPID int, 
@LocationID int,
@StaffID int,
@FromDate Date,
@ToDate date
as
Begin

select 
	  [StaffID]
      ,[UserName]
      ,[InTime]
      ,[OutTime]
	  ,(TotalWorkingTime-(IdelTime+LogOutTime)) AS [NetWorkingTime]
      ,[TotalWorkingTime]
      ,[IdelTime]
      ,[LogOutTime]
      ,[LoginDate]
      ,[ContraryName]
      ,[LocationName]
      ,[Designation]
      ,[ShiftTime]
      ,[DepartmentName]
      ,[LocationMasterId]
	  ,SectionPID
	  ,ContraryPID
	  from (
select 
	   [StaffID]
      ,[UserName]
      ,Min([InTime]) as InTime
      ,Max([OutTime]) as OutTime
	  ,datediff(MI,Min([InTime]),Max([OutTime]) ) as [TotalWorkingTime]
	  ,sum(case when [Event]='IdleTime' then  datediff(MI,[InTime],[OutTime] )  else 0 end)  as IdelTime
	  ,sum ( case when  [OldEvent]='OnSessionChange'  and [OutTime] is not null then dbo.LogOutTime(TransectionId) else 0 end )/60 as LogOutTime
      ,[LoginDate]
      ,[ContraryName]
      ,[LocationName]
      ,[Designation]
      ,[ShiftTime]
      ,[DepartmentName]
      ,[LocationMasterId]
	  ,SectionPID
	  ,ContraryPID
 
 from (
select 
      t.TransectionId
	  ,s.StaffID
      ,isnull(s.StaffName,[UserName]) as [UserName]
	  ,case when ([Event]='OnStart') then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionLogon')  then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionUnlock')  then [LogoutTime] 
			when ([Event]='IdleTime')  then [LoginTime] 
            Else null
	   End as InTime
      ,case when ([Event]='OnSessionChange' and   [Reason] ='SessionLogoff') then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionLock')  then [LogoutTime] 
			when ([Event]='IdleTime' )  then [LogoutTime] 
            Else null
	   End as OutTime
      ,[LoginDate]
	  ,Case when [Event]='OnSessionChange' Then Reason else [Event] end  as [Event]
	  ,[Event] as OldEvent
	  ,pv1.PicklistValueName as ContraryName
	  ,l.LocationName
	  ,pv.PicklistValueName as Designation
	  ,pv2.PicklistValueName as  ShiftTime
	  ,pv2.PicklistValueName as  DepartmentName
	  ,s.OFCLoc as LocationMasterId
	  ,s.SectionPID
	  ,l.ContraryPID
     from Transection t 
	 left join [Staff] s on s.StaffUserName = t.UserName
	 left join LocationMaster l on l.LocationMasterID= s.OFCLoc
	 left join PickListValue pv on pv.PicklistValueID= s.DesignationPID
	 left join PickListValue pv1 on pv1.PicklistValueID= l.ContraryPID
	 left join PickListValue pv2 on pv2.PicklistValueID= s.OFCTime
	 left join PickListValue pv3 on pv3.PicklistValueID= s.SectionPID
	 where 
	   t.[LoginDate]  between @FromDate and @ToDate  and
	   isnull( s.OFCLoc,0)  = case when @LocationID=0  then     isnull(s.OFCLoc,0)  else @LocationID end  and 
	     isnull(l.ContraryPID,0)   = case when @ContraryPID=0 then     isnull(l.ContraryPID,0)   else @ContraryPID end  and 
	     isnull(s.SectionPID,0)  = case when @LocationID=0 then     isnull(s.SectionPID,0)  else @LocationID end  
	 ) E

	 group by 
	 [StaffID]
	  ,[UserName]
      ,[LoginDate]
      ,[ContraryName]
      ,[LocationName]
      ,[Designation]
      ,[ShiftTime]
      ,[DepartmentName]
      ,[LocationMasterId]
	  ,SectionPID
	  ,ContraryPID
	  ) S order by LoginDate 
END






Create FUNCTION [dbo].[LogOutTime](@TransectionId int )  
RETURNS int   
AS   
-- Returns the stock level for the product.  
BEGIN  
   Declare @UserName Varchar(100) 
   Declare @LoginDate Date 
   Declare @OutTime Time(7) 
   Declare @InTime Time (7)
   
   select @UserName = UserName, @LoginDate= LoginDate, @OutTime=LogoutTime from Transection where TransectionId=@TransectionId

 set   @InTime = (select top 1  LogoutTime
								from Transection where UserName =@UserName
								 and LogoutTime > @OutTime 
								 and LoginDate =@LoginDate 
								 and [Event]='OnSessionChange'
								order by LogoutTime	
				  )
				   
   return datediff(SECOND,@OutTime,@InTime)

END




ALTER PROCEDURE [dbo].[ReadStaffDailyAttendanceReport]

     @StaffID int,
	 @ContraryPID int,
	 @LocationID int
AS
BEGIN

if(@StaffID=0)

select [TransectionId]
      ,[ClientGuid]
      , isnull(s.StaffName,[UserName]) as [UserName]
	  ,case when ([Event]='OnStart') then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionLogon')  then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionUnlock')  then [LogoutTime] 
			when ([Event]='IdleTime')  then [LoginTime] 
            Else null
	   End as InTime
      ,case when ([Event]='OnSessionChange' and   [Reason] ='SessionLogoff') then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionLock')  then [LogoutTime] 
			when ([Event]='IdleTime' )  then [LogoutTime] 
            Else null
	   End as OutTime
      ,[LoginDate]
      ,[TotalHours]
      ,Case when [Event]='OnSessionChange' Then Reason else [Event] end  as [Event]
      ,[Reason]
      ,[IsManual]
      ,t.[CreatedBy]
      ,t.[CreatedOn]
      ,t.[ModifiedBy]
      ,t.[ModifiedOn]
     from Transection t 
	 left join [Staff] s on s.StaffUserName = t.UserName
	 left join LocationMaster l on l.LocationMasterID= @LocationID
	 where case when @LocationID=0 then 0 else isnull (s.LocationMasterID,0) end = @LocationID
	 and case when @ContraryPID=0 then 0 else isnull (l.ContraryPID,0) end = @ContraryPID

	  order by UserName, LoginDate

	  else 

	  select [TransectionId]
      ,[ClientGuid]
      , isnull(s.StaffName,[UserName]) as [UserName]
	  ,case when ([Event]='OnStart') then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionLogon')  then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionUnlock')  then [LogoutTime] 
			when ([Event]='IdleTime')  then [LoginTime] 
            Else null
	   End as InTime
      ,case when ([Event]='OnSessionChange' and   [Reason] ='SessionLogoff') then [LogoutTime] 
			when ([Event]='OnSessionChange' and   [Reason] ='SessionLock')  then [LogoutTime] 
			when ([Event]='IdleTime' )  then [LogoutTime] 
            Else null
	   End as OutTime
      ,[LoginDate]
      ,[TotalHours]
      ,Case when [Event]='OnSessionChange' Then Reason else [Event] end  as [Event]
      ,[Reason]
      ,[IsManual]
      ,t.[CreatedBy]
      ,t.[CreatedOn]
      ,t.[ModifiedBy]
      ,t.[ModifiedOn]
      from Transection t 
	 left join [Staff] s on s.StaffUserName = t.UserName
	 left join LocationMaster l on l.LocationMasterID= @LocationID
	 where case when @LocationID=0 then 0 else isnull (s.LocationMasterID,0) end = case when s.LocationMasterID is null then 0 else  @LocationID end
	 and case when @ContraryPID=0 then 0 else isnull (l.ContraryPID,0) end = case when l.ContraryPID is null then 0 else  @ContraryPID end
	  and case when @StaffID=0 then 0 else isnull (s.StaffID,0) end = case when s.StaffID is null then 0 else @StaffID end

	  order by UserName, LoginDate

END


